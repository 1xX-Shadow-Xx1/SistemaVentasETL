using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SistemaVentas.Application.Interfaces;
using SistemaVentas.Data.Models;

namespace SistemaVentas.Persistence.Repositories.Dwh
{
    public class DwhRepository : IDwhRepository
    {
        private readonly string _dwhConnectionString;
        private readonly string _ventasConnectionString;
        private readonly IClienteApiRepository _apiRepository;
        private readonly ICsvVentasRepository _csvRepository;

        public DwhRepository(
            IConfiguration configuration,
            IClienteApiRepository apiRepository,
            ICsvVentasRepository csvRepository)
        {
            _dwhConnectionString = configuration.GetConnectionString("DwhDatabase")
                ?? throw new ArgumentNullException("ConnectionStrings:DwhDatabase no configurado");
            _ventasConnectionString = configuration.GetConnectionString("VentasDatabase")
                ?? throw new ArgumentNullException("ConnectionStrings:VentasDatabase no configurado");
            _apiRepository = apiRepository;
            _csvRepository = csvRepository;
        }

        public async Task LoadVentasDataAsync()
        {
            // 1. Extract from all 3 sources
            var apiCustomers = await _apiRepository.GetCustomersAsync();
            var csvVentas = await _csvRepository.GetVentasAsync();
            IEnumerable<Customer> dbCustomers;
            IEnumerable<Order> dbOrders;
            IEnumerable<OrderDetail> dbOrderDetails;
            IEnumerable<Product> dbProducts;
            IEnumerable<City> dbCities;
            IEnumerable<Country> dbCountries;

            using var ventasConn = new SqlConnection(_ventasConnectionString);
            dbCustomers = await ventasConn.QueryAsync<Customer>("SELECT * FROM Customer");
            dbOrders    = await ventasConn.QueryAsync<Order>("SELECT * FROM [Order]");
            dbOrderDetails = await ventasConn.QueryAsync<OrderDetail>("SELECT * FROM Order_Detail");
            dbProducts  = await ventasConn.QueryAsync<Product>("SELECT * FROM Product");
            dbCities    = await ventasConn.QueryAsync<City>("SELECT * FROM City");
            dbCountries = await ventasConn.QueryAsync<Country>("SELECT * FROM Country");

            // 2. Load into DWH using Dapper
            using var dwhConn = new SqlConnection(_dwhConnectionString);
            await dwhConn.OpenAsync();

            // 2a. Dim_Cliente - from DB + API (merge: API is enrichment, DB is source of truth)
            foreach (var cust in dbCustomers)
            {
                var apiMatch = apiCustomers.FirstOrDefault(a => a.Id == cust.CustomerId);
                var sql = @"MERGE Dim_Cliente AS target
                    USING (SELECT @ID_Cliente, @Nombre_Cliente, @Tipo_Cliente) AS source (ID_Cliente, Nombre_Cliente, Tipo_Cliente)
                    ON target.ID_Cliente = source.ID_Cliente
                    WHEN MATCHED THEN UPDATE SET Nombre_Cliente = source.Nombre_Cliente, Tipo_Cliente = source.Tipo_Cliente
                    WHEN NOT MATCHED THEN INSERT (ID_Cliente, Nombre_Cliente, Tipo_Cliente) VALUES (source.ID_Cliente, source.Nombre_Cliente, source.Tipo_Cliente);";
                await dwhConn.ExecuteAsync(sql, new
                {
                    ID_Cliente = cust.CustomerId,
                    Nombre_Cliente = $"{cust.FirstName} {cust.LastName}",
                    Tipo_Cliente = apiMatch?.CustomerType ?? "Regular"
                });
            }

            // 2b. Dim_Producto
            foreach (var prod in dbProducts)
            {
                var sql = @"MERGE Dim_Producto AS target
                    USING (SELECT @ID_Producto, @Nombre_Producto, @Categoria, @Precio_Base) AS source (ID_Producto, Nombre_Producto, Categoria, Precio_Base)
                    ON target.ID_Producto = source.ID_Producto
                    WHEN MATCHED THEN UPDATE SET Nombre_Producto = source.Nombre_Producto, Categoria = source.Categoria, Precio_Base = source.Precio_Base
                    WHEN NOT MATCHED THEN INSERT (ID_Producto, Nombre_Producto, Categoria, Precio_Base) VALUES (source.ID_Producto, source.Nombre_Producto, source.Categoria, source.Precio_Base);";
                await dwhConn.ExecuteAsync(sql, new
                {
                    ID_Producto = prod.ProductId,
                    Nombre_Producto = prod.ProductName,
                    Categoria = prod.CategoryId.ToString(),
                    Precio_Base = prod.Price
                });
            }

            // 2c. Dim_Ubicacion (from DB City/Country)
            foreach (var city in dbCities)
            {
                var country = dbCountries.FirstOrDefault(c => c.CountryId == city.CountryId);
                var sql = @"MERGE Dim_Ubicacion AS target
                    USING (SELECT @ID_Ubicacion, @Pais, @Region, @Ciudad) AS source (ID_Ubicacion, Pais, Region, Ciudad)
                    ON target.ID_Ubicacion = source.ID_Ubicacion
                    WHEN MATCHED THEN UPDATE SET Pais = source.Pais, Region = source.Region, Ciudad = source.Ciudad
                    WHEN NOT MATCHED THEN INSERT (ID_Ubicacion, Pais, Region, Ciudad) VALUES (source.ID_Ubicacion, source.Pais, source.Region, source.Ciudad);";
                await dwhConn.ExecuteAsync(sql, new
                {
                    ID_Ubicacion = city.CityId,
                    Pais = country?.CountryName ?? "Desconocido",
                    Region = country?.CountryName ?? "Desconocido",
                    Ciudad = city.CityName
                });
            }

            // 2d. Dim_Tiempo + Fact_Ventas from Orders+OrderDetails
            foreach (var order in dbOrders)
            {
                var orderDate = order.OrderDate ?? DateTime.Today;
                int timeId = int.Parse(orderDate.ToString("yyyyMMdd"));

                // Dim_Tiempo
                var timeSql = @"MERGE Dim_Tiempo AS target
                    USING (SELECT @ID_Tiempo, @Fecha, @Anio, @Trimestre, @Mes, @Nombre_Mes, @Dia) AS source (ID_Tiempo, Fecha, Anio, Trimestre, Mes, Nombre_Mes, Dia)
                    ON target.ID_Tiempo = source.ID_Tiempo
                    WHEN NOT MATCHED THEN INSERT (ID_Tiempo, Fecha, Anio, Trimestre, Mes, Nombre_Mes, Dia) VALUES (source.ID_Tiempo, source.Fecha, source.Anio, source.Trimestre, source.Mes, source.Nombre_Mes, source.Dia);";
                await dwhConn.ExecuteAsync(timeSql, new
                {
                    ID_Tiempo = timeId,
                    Fecha = orderDate.Date,
                    Anio = orderDate.Year,
                    Trimestre = (orderDate.Month - 1) / 3 + 1,
                    Mes = orderDate.Month,
                    Nombre_Mes = orderDate.ToString("MMMM"),
                    Dia = orderDate.Day
                });

                // Fact_Ventas (one row per order detail)
                var details = dbOrderDetails.Where(d => d.OrderId == order.OrderId);
                var customer = dbCustomers.FirstOrDefault(c => c.CustomerId == order.CustomerId);
                var ubicacionId = customer != null ? (customer.CityId ?? 0) : 0;

                foreach (var detail in details)
                {
                    var factSql = @"INSERT INTO Fact_Ventas (ID_Transaccion, ID_Tiempo, ID_Producto, ID_Cliente, ID_Ubicacion, Cantidad, Precio_Unitario, Total_Venta, Origen_Datos)
                        VALUES (@ID_Transaccion, @ID_Tiempo, @ID_Producto, @ID_Cliente, @ID_Ubicacion, @Cantidad, @Precio_Unitario, @Total_Venta, @Origen_Datos)";
                    await dwhConn.ExecuteAsync(factSql, new
                    {
                        ID_Transaccion = order.OrderId,
                        ID_Tiempo = timeId,
                        ID_Producto = detail.ProductId,
                        ID_Cliente = order.CustomerId,
                        ID_Ubicacion = ubicacionId,
                        Cantidad = detail.Quantity,
                        Precio_Unitario = detail.UnitPrice,
                        Total_Venta = detail.TotalPrice,
                        Origen_Datos = "DB"
                    });
                }
            }

            // 2e. Fact_Ventas from CSV (additional sales not in DB)
            foreach (var venta in csvVentas)
            {
                var orderDate = venta.OrderDate;
                int timeId = int.Parse(orderDate.ToString("yyyyMMdd"));

                var timeSql = @"MERGE Dim_Tiempo AS target
                    USING (SELECT @ID_Tiempo, @Fecha, @Anio, @Trimestre, @Mes, @Nombre_Mes, @Dia) AS source (ID_Tiempo, Fecha, Anio, Trimestre, Mes, Nombre_Mes, Dia)
                    ON target.ID_Tiempo = source.ID_Tiempo
                    WHEN NOT MATCHED THEN INSERT (ID_Tiempo, Fecha, Anio, Trimestre, Mes, Nombre_Mes, Dia) VALUES (source.ID_Tiempo, source.Fecha, source.Anio, source.Trimestre, source.Mes, source.Nombre_Mes, source.Dia);";
                await dwhConn.ExecuteAsync(timeSql, new
                {
                    ID_Tiempo = timeId,
                    Fecha = orderDate.Date,
                    Anio = orderDate.Year,
                    Trimestre = (orderDate.Month - 1) / 3 + 1,
                    Mes = orderDate.Month,
                    Nombre_Mes = orderDate.ToString("MMMM"),
                    Dia = orderDate.Day
                });

                var factSql = @"INSERT INTO Fact_Ventas (ID_Transaccion, ID_Tiempo, ID_Producto, ID_Cliente, ID_Ubicacion, Cantidad, Precio_Unitario, Total_Venta, Origen_Datos)
                    VALUES (@ID_Transaccion, @ID_Tiempo, @ID_Producto, @ID_Cliente, @ID_Ubicacion, @Cantidad, @Precio_Unitario, @Total_Venta, @Origen_Datos)";
                await dwhConn.ExecuteAsync(factSql, new
                {
                    ID_Transaccion = venta.OrderID,
                    ID_Tiempo = timeId,
                    ID_Producto = 0,             // OrderCsv does not carry ProductID; join via order_details if needed
                    ID_Cliente = venta.CustomerID,
                    ID_Ubicacion = 0,            // No city info in CSV
                    Cantidad = 0,
                    Precio_Unitario = 0m,
                    Total_Venta = 0m,
                    Origen_Datos = "CSV"
                });
            }
        }
    }
}
