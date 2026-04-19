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

            // ─── 2. SIN TRUNCATE (Carga Incremental) ──────────────────────
            using var dwhConn = new SqlConnection(_dwhConnectionString);
            await dwhConn.OpenAsync();
            Console.WriteLine("[ETL] Iniciando proceso incremental (Upsert).");

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

            // Cargar sets de IDs válidos para validación estricta
            var validClientes = (await dwhConn.QueryAsync<int>("SELECT ID_Cliente FROM Dim_Cliente")).ToHashSet();
            var validProductos = (await dwhConn.QueryAsync<int>("SELECT ID_Producto FROM Dim_Producto")).ToHashSet();
            var validUbicaciones = (await dwhConn.QueryAsync<int>("SELECT ID_Ubicacion FROM Dim_Ubicacion")).ToHashSet();

            int dbProcessed = 0, csvProcessed = 0, apiProcessed = 0;
            int dbSkipped = 0, csvSkipped = 0, apiSkipped = 0;

            // ─── 4. FACT_VENTAS — DB ──────────────────────────────────────
            Console.WriteLine("[ETL] Procesando Fact_Ventas (DB)...");
            foreach (var order in dbOrders)
            {
                try
                {
                    var orderDate = order.OrderDate ?? DateTime.Today;
                    int timeId = int.Parse(orderDate.ToString("yyyyMMdd"));
                    var customer = dbCustomers.FirstOrDefault(c => c.CustomerId == order.CustomerId);
                    
                    int cid = order.CustomerId ?? 0;
                    int uid = customer?.CityId ?? 0;

                    // Validación estricta: Cliente y Ubicación deben existir
                    if (!validClientes.Contains(cid) || !validUbicaciones.Contains(uid)) { dbSkipped++; continue; }

                    await MergeDimTiempo(dwhConn, timeId, orderDate);

                    foreach (var detail in dbOrderDetails.Where(d => d.OrderId == order.OrderId))
                    {
                        int pid = detail.ProductId;
                        if (!validProductos.Contains(pid)) { dbSkipped++; continue; }

                        await UpsertFactVentas(dwhConn, order.OrderId, timeId, pid,
                            cid, uid, detail.Quantity,
                            detail.UnitPrice ?? 0m, detail.TotalPrice ?? 0m, "DB");
                        dbProcessed++;
                    }
                }
                catch (Exception ex) { Console.WriteLine($"[ETL WARN] DB Order {order.OrderId} failed: {ex.Message}"); }
            }
            Console.WriteLine($"[ETL] DB → Procesados/Upsert: {dbProcessed}, Omitidos: {dbSkipped}");

            // ─── 5. FACT_VENTAS — CSV ─────────────────────────────────────
            Console.WriteLine("[ETL] Procesando Fact_Ventas (CSV)...");
            foreach (var order in csvOrders)
            {
                try
                {
                    var orderDate = order.OrderDate;
                    int timeId = int.Parse(orderDate.ToString("yyyyMMdd"));

                    int cid = order.CustomerID;
                    // En CSV no tenemos info de ubicación, omitimos si la tabla DWH requiere integridad (y no manejamos ID 0)
                    if (!validClientes.Contains(cid)) { csvSkipped++; continue; }

                    await MergeDimTiempo(dwhConn, timeId, orderDate);

                    var details = csvOrderDetails.Where(d => d.OrderID == order.OrderID).ToList();
                    if (details.Count > 0)
                    {
                        foreach (var detail in details)
                        {
                            int pid = detail.ProductID;
                            if (!validProductos.Contains(pid)) { csvSkipped++; continue; }

                            await UpsertFactVentas(dwhConn, order.OrderID, timeId, pid,
                                cid, 0, detail.Quantity,
                                0m, detail.TotalPrice, "CSV");
                            csvProcessed++;
                        }
                    }
                }
                catch (Exception ex) { Console.WriteLine($"[ETL WARN] CSV Order {order.OrderID} failed: {ex.Message}"); }
            }
            Console.WriteLine($"[ETL] CSV → Procesados/Upsert: {csvProcessed}, Omitidos: {csvSkipped}");

            // ─── 6. FACT_VENTAS — API ─────────────────────────────────────
            Console.WriteLine("[ETL] Procesando Fact_Ventas (API)...");
            foreach (var order in apiOrders)
            {
                try
                {
                    DateTime.TryParse(order.OrderDate, out DateTime orderDate);
                    if (orderDate == default) orderDate = DateTime.Today;
                    int timeId = int.Parse(orderDate.ToString("yyyyMMdd"));

                    int cid = order.CustomerId;
                    if (!validClientes.Contains(cid)) { apiSkipped++; continue; }

                    await MergeDimTiempo(dwhConn, timeId, orderDate);

                    var apiDetails = apiOrderDetails.Where(d => d.OrderId == order.OrderId).ToList();
                    if (apiDetails.Count > 0)
                    {
                        foreach (var detail in apiDetails)
                        {
                            int pid = detail.ProductId;
                            if (!validProductos.Contains(pid)) { apiSkipped++; continue; }

                            await UpsertFactVentas(dwhConn, order.OrderId, timeId, pid,
                                cid, 0, detail.Quantity,
                                detail.UnitPrice, detail.TotalPrice, "API");
                            apiProcessed++;
                        }
                    }
                }
                catch (Exception ex) { Console.WriteLine($"[ETL WARN] API Order {order.OrderId} failed: {ex.Message}"); }
            }
            Console.WriteLine($"[ETL] API → Procesados/Upsert: {apiProcessed}, Omitidos: {apiSkipped}");

            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"[ETL COMPLETO] Total DB: {dbProcessed}, CSV: {csvProcessed}, API: {apiProcessed}");
            Console.WriteLine($"[CALIDAD] Registros omitidos: {dbSkipped + csvSkipped + apiSkipped}");
            Console.WriteLine("--------------------------------------------------");
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

        private static async Task UpsertFactVentas(SqlConnection conn,
            int transId, int timeId, int productId, int clienteId, int ubicacionId,
            int cantidad, decimal unitPrice, decimal totalPrice, string origen)
        {
            await conn.ExecuteAsync(@"
                MERGE Fact_Ventas AS t
                USING (SELECT @ID_Transaccion, @ID_Tiempo, @ID_Producto, @ID_Cliente, @ID_Ubicacion, 
                              @Cantidad, @Precio_Unitario, @Total_Venta, @Origen_Datos) 
                      AS s (ID_Transaccion, ID_Tiempo, ID_Producto, ID_Cliente, ID_Ubicacion, 
                            Cantidad, Precio_Unitario, Total_Venta, Origen_Datos)
                ON t.ID_Transaccion = s.ID_Transaccion AND t.Origen_Datos = s.Origen_Datos AND t.ID_Producto = s.ID_Producto
                WHEN MATCHED THEN
                    UPDATE SET ID_Tiempo = s.ID_Tiempo, ID_Cliente = s.ID_Cliente, ID_Ubicacion = s.ID_Ubicacion,
                               Cantidad = s.Cantidad, Precio_Unitario = s.Precio_Unitario, Total_Venta = s.Total_Venta
                WHEN NOT MATCHED THEN
                    INSERT (ID_Transaccion, ID_Tiempo, ID_Producto, ID_Cliente, ID_Ubicacion,
                            Cantidad, Precio_Unitario, Total_Venta, Origen_Datos)
                    VALUES (s.ID_Transaccion, s.ID_Tiempo, s.ID_Producto, s.ID_Cliente, s.ID_Ubicacion,
                            s.Cantidad, s.Precio_Unitario, s.Total_Venta, s.Origen_Datos);",
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
