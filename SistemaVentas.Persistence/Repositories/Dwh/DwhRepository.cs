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
            // ─── 1. EXTRACT ────────────────────────────────────────────────
            var apiCustomers    = await _apiRepository.GetCustomersAsync();
            var apiOrders       = await _apiRepository.GetOrdersAsync();
            var apiOrderDetails = await _apiRepository.GetOrderDetailsAsync();

            var csvOrders       = await _csvRepository.GetVentasAsync();
            var csvOrderDetails = await _csvRepository.GetOrderDetailsAsync();

            IEnumerable<Customer>    dbCustomers;
            IEnumerable<Order>       dbOrders;
            IEnumerable<OrderDetail> dbOrderDetails;
            IEnumerable<Product>     dbProducts;
            IEnumerable<City>        dbCities;
            IEnumerable<Country>     dbCountries;

            using var ventasConn = new SqlConnection(_ventasConnectionString);
            dbCustomers    = await ventasConn.QueryAsync<Customer>   ("SELECT * FROM Customer");
            dbOrders       = await ventasConn.QueryAsync<Order>      ("SELECT * FROM [Order]");
            dbOrderDetails = await ventasConn.QueryAsync<OrderDetail> ("SELECT * FROM Order_Detail");
            dbProducts     = await ventasConn.QueryAsync<Product>    ("SELECT * FROM Product");
            dbCities       = await ventasConn.QueryAsync<City>       ("SELECT * FROM City");
            dbCountries    = await ventasConn.QueryAsync<Country>    ("SELECT * FROM Country");

            // ─── DIAGNÓSTICO: conteo por fuente ───────────────────────────
            Console.WriteLine($"[ETL] API  → Órdenes: {apiOrders.Count()}, Detalles: {apiOrderDetails.Count()}");
            Console.WriteLine($"[ETL] CSV  → Órdenes: {csvOrders.Count()}, Detalles: {csvOrderDetails.Count()}");
            Console.WriteLine($"[ETL] DB   → Órdenes: {dbOrders.Count()}, Detalles: {dbOrderDetails.Count()}");

            // ─── 2. TRUNCAR Fact_Ventas (ETL limpio cada vez) ─────────────
            using var dwhConn = new SqlConnection(_dwhConnectionString);
            await dwhConn.OpenAsync();
            await dwhConn.ExecuteAsync("TRUNCATE TABLE Fact_Ventas");

            // Dim_Cliente (DB + API enrichment)
            foreach (var cust in dbCustomers)
            {
                var apiMatch = apiCustomers.FirstOrDefault(a => a.Id == cust.CustomerId);
                await dwhConn.ExecuteAsync(@"
                    MERGE Dim_Cliente AS t
                    USING (SELECT @ID_Cliente, @Nombre_Cliente, @Tipo_Cliente) AS s (ID_Cliente, Nombre_Cliente, Tipo_Cliente)
                    ON t.ID_Cliente = s.ID_Cliente
                    WHEN MATCHED     THEN UPDATE SET Nombre_Cliente = s.Nombre_Cliente, Tipo_Cliente = s.Tipo_Cliente
                    WHEN NOT MATCHED THEN INSERT (ID_Cliente, Nombre_Cliente, Tipo_Cliente)
                                          VALUES (s.ID_Cliente, s.Nombre_Cliente, s.Tipo_Cliente);",
                    new
                    {
                        ID_Cliente     = cust.CustomerId,
                        Nombre_Cliente = $"{cust.FirstName} {cust.LastName}",
                        Tipo_Cliente   = apiMatch?.CustomerType ?? "Regular"
                    });
            }

            // Dim_Producto
            foreach (var prod in dbProducts)
            {
                await dwhConn.ExecuteAsync(@"
                    MERGE Dim_Producto AS t
                    USING (SELECT @ID_Producto, @Nombre_Producto, @Categoria, @Precio_Base) AS s (ID_Producto, Nombre_Producto, Categoria, Precio_Base)
                    ON t.ID_Producto = s.ID_Producto
                    WHEN MATCHED     THEN UPDATE SET Nombre_Producto = s.Nombre_Producto, Categoria = s.Categoria, Precio_Base = s.Precio_Base
                    WHEN NOT MATCHED THEN INSERT (ID_Producto, Nombre_Producto, Categoria, Precio_Base)
                                          VALUES (s.ID_Producto, s.Nombre_Producto, s.Categoria, s.Precio_Base);",
                    new
                    {
                        ID_Producto    = prod.ProductId,
                        Nombre_Producto = prod.ProductName,
                        Categoria      = prod.CategoryId.ToString(),
                        Precio_Base    = prod.Price
                    });
            }

            // Dim_Ubicacion
            foreach (var city in dbCities)
            {
                var country = dbCountries.FirstOrDefault(c => c.CountryId == city.CountryId);
                await dwhConn.ExecuteAsync(@"
                    MERGE Dim_Ubicacion AS t
                    USING (SELECT @ID_Ubicacion, @Pais, @Region, @Ciudad) AS s (ID_Ubicacion, Pais, Region, Ciudad)
                    ON t.ID_Ubicacion = s.ID_Ubicacion
                    WHEN MATCHED     THEN UPDATE SET Pais = s.Pais, Region = s.Region, Ciudad = s.Ciudad
                    WHEN NOT MATCHED THEN INSERT (ID_Ubicacion, Pais, Region, Ciudad)
                                          VALUES (s.ID_Ubicacion, s.Pais, s.Region, s.Ciudad);",
                    new
                    {
                        ID_Ubicacion = city.CityId,
                        Pais         = country?.CountryName ?? "Desconocido",
                        Region       = country?.CountryName ?? "Desconocido",
                        Ciudad       = city.CityName
                    });
            }

            // ─── 3. FACT_VENTAS — DB ──────────────────────────────────────
            foreach (var order in dbOrders)
            {
                var orderDate    = order.OrderDate ?? DateTime.Today;
                int timeId       = int.Parse(orderDate.ToString("yyyyMMdd"));
                var customer     = dbCustomers.FirstOrDefault(c => c.CustomerId == order.CustomerId);
                var ubicacionId  = customer?.CityId ?? 0;

                await MergeDimTiempo(dwhConn, timeId, orderDate);

                foreach (var detail in dbOrderDetails.Where(d => d.OrderId == order.OrderId))
                {
                    await InsertFactVentas(dwhConn, order.OrderId, timeId, detail.ProductId,
                        order.CustomerId ?? 0, ubicacionId, detail.Quantity,
                        detail.UnitPrice ?? 0m, detail.TotalPrice ?? 0m, "DB");
                }
            }

            // ─── 4. FACT_VENTAS — CSV ─────────────────────────────────────
            foreach (var order in csvOrders)
            {
                var orderDate   = order.OrderDate;
                int timeId      = int.Parse(orderDate.ToString("yyyyMMdd"));

                await MergeDimTiempo(dwhConn, timeId, orderDate);

                var details = csvOrderDetails.Where(d => d.OrderID == order.OrderID).ToList();
                if (details.Count > 0)
                {
                    // Insert one row per detail line
                    foreach (var detail in details)
                    {
                        await InsertFactVentas(dwhConn, order.OrderID, timeId, detail.ProductID,
                            order.CustomerID, 0, detail.Quantity,
                            0m, detail.TotalPrice, "CSV");
                    }
                }
                else
                {
                    // No matching details — insert the order itself as a summary row
                    await InsertFactVentas(dwhConn, order.OrderID, timeId, 0,
                        order.CustomerID, 0, 0, 0m, 0m, "CSV");
                }
            }

            // ─── 5. FACT_VENTAS — API ─────────────────────────────────────
            foreach (var order in apiOrders)
            {
                DateTime.TryParse(order.OrderDate, out DateTime orderDate);
                if (orderDate == default) orderDate = DateTime.Today;
                int timeId = int.Parse(orderDate.ToString("yyyyMMdd"));

                await MergeDimTiempo(dwhConn, timeId, orderDate);

                var apiDetails = apiOrderDetails.Where(d => d.OrderId == order.OrderId).ToList();
                if (apiDetails.Count > 0)
                {
                    foreach (var detail in apiDetails)
                    {
                        await InsertFactVentas(dwhConn, order.OrderId, timeId, detail.ProductId,
                            order.CustomerId, 0, detail.Quantity,
                            detail.UnitPrice, detail.TotalPrice, "API");
                    }
                }
                else
                {
                    // No details from API — insert the order as a summary row
                    await InsertFactVentas(dwhConn, order.OrderId, timeId, 0,
                        order.CustomerId, 0, 0, 0m, 0m, "API");
                }
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────
        private static async Task MergeDimTiempo(SqlConnection conn, int timeId, DateTime date)
        {
            await conn.ExecuteAsync(@"
                MERGE Dim_Tiempo AS t
                USING (SELECT @ID_Tiempo, @Fecha, @Anio, @Trimestre, @Mes, @Nombre_Mes, @Dia)
                      AS s (ID_Tiempo, Fecha, Anio, Trimestre, Mes, Nombre_Mes, Dia)
                ON t.ID_Tiempo = s.ID_Tiempo
                WHEN NOT MATCHED THEN
                    INSERT (ID_Tiempo, Fecha, Anio, Trimestre, Mes, Nombre_Mes, Dia)
                    VALUES (s.ID_Tiempo, s.Fecha, s.Anio, s.Trimestre, s.Mes, s.Nombre_Mes, s.Dia);",
                new
                {
                    ID_Tiempo  = timeId,
                    Fecha      = date.Date,
                    Anio       = date.Year,
                    Trimestre  = (date.Month - 1) / 3 + 1,
                    Mes        = date.Month,
                    Nombre_Mes = date.ToString("MMMM"),
                    Dia        = date.Day
                });
        }

        private static async Task InsertFactVentas(SqlConnection conn,
            int transId, int timeId, int productId, int clienteId, int ubicacionId,
            int cantidad, decimal unitPrice, decimal totalPrice, string origen)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO Fact_Ventas
                    (ID_Transaccion, ID_Tiempo, ID_Producto, ID_Cliente, ID_Ubicacion,
                     Cantidad, Precio_Unitario, Total_Venta, Origen_Datos)
                VALUES
                    (@ID_Transaccion, @ID_Tiempo, @ID_Producto, @ID_Cliente, @ID_Ubicacion,
                     @Cantidad, @Precio_Unitario, @Total_Venta, @Origen_Datos)",
                new
                {
                    ID_Transaccion = transId,
                    ID_Tiempo      = timeId,
                    ID_Producto    = productId,
                    ID_Cliente     = clienteId,
                    ID_Ubicacion   = ubicacionId,
                    Cantidad       = cantidad,
                    Precio_Unitario = unitPrice,
                    Total_Venta    = totalPrice,
                    Origen_Datos   = origen
                });
        }
    }
}
