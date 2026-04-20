using Microsoft.EntityFrameworkCore;
using SistemaVentas.Application.Interfaces;
using SistemaVentas.Domain.Entities.Dwh.Dimensions;
using SistemaVentas.Domain.Entities.Dwh.Facts;
using SistemaVentas.Persistence.Repositories.Dwh.Context;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Dapper;
using SistemaVentas.Domain.Entities.Csv;
using SistemaVentas.Domain.Entities.Api;
using SistemaVentas.Data.Models;
using SistemaVentas.Application.Result;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace SistemaVentas.Persistence.Repositories.Dwh
{
    public class DwhRepository : IDwhRepository
    {
        private readonly VentasDwhContext _context;
        private readonly IConfiguration _configuration;
        private readonly IClienteApiRepository _apiRepository;
        private readonly ICsvVentasRepository _csvRepository;
        private readonly ILogger<DwhRepository> _logger;

        public DwhRepository(
            VentasDwhContext context,
            IConfiguration configuration,
            IClienteApiRepository apiRepository,
            ICsvVentasRepository csvRepository,
            ILogger<DwhRepository> logger)
        {
            _context = context;
            _configuration = configuration;
            _apiRepository = apiRepository;
            _csvRepository = csvRepository;
            _logger = logger;
        }

        private record ExtractedData(
            IEnumerable<CustomerAPIDto> ApiCustomers,
            IEnumerable<OrderAPIDto> ApiOrders,
            IEnumerable<OrderDetailAPIDto> ApiOrderDetails,
            IEnumerable<OrderCsv> CsvOrders,
            IEnumerable<OrderDetailCsv> CsvOrderDetails,
            IEnumerable<CustomerCsv> CsvCustomers,
            IEnumerable<Customer> DbCustomers,
            IEnumerable<Order> DbOrders,
            IEnumerable<OrderDetail> DbOrderDetails,
            IEnumerable<Product> DbProducts,
            IEnumerable<City> DbCities,
            IEnumerable<Country> DbCountries
        );

        public async Task<ServiceResult> LoadVentasDataAsync()
        {
            var result = new ServiceResult();
            try
            {
                _logger.LogInformation("[ETL] Proceso iniciado conforme al patrón del profesor.");

                var data = await ExtractDataSourcesAsync();

                result = await CleanDwhTablesAsync();
                if (!result.IsSuccess) return result;

                await LoadDimensionsAsync(data);

                result = await LoadFactVentasAsync(data);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = $"Error crítico en el proceso ETL: {ex.Message}";
                _logger.LogError(ex, "[ETL] ERROR: {Message}", ex.Message);
            }

            return result;
        }

        private async Task<ExtractedData> ExtractDataSourcesAsync()
        {
            _logger.LogInformation("[ETL] Extrayendo datos de todas las fuentes...");

            var apiCustomers = await _apiRepository.GetCustomersAsync();
            var apiOrders = await _apiRepository.GetOrdersAsync();
            var apiOrderDetails = await _apiRepository.GetOrderDetailsAsync();
            var csvOrders = await _csvRepository.GetVentasAsync();
            var csvOrderDetails = await _csvRepository.GetOrderDetailsAsync();
            var csvCustomers = await _csvRepository.GetCustomersAsync();

            using var salesConn = new SqlConnection(_configuration.GetConnectionString("VentasDatabase"));
            var dbCustomers = await salesConn.QueryAsync<Customer>("SELECT * FROM Customer");
            var dbOrders = await salesConn.QueryAsync<Order>("SELECT * FROM [Order]");
            var dbOrderDetails = await salesConn.QueryAsync<OrderDetail>("SELECT * FROM Order_Detail");
            var dbProducts = await salesConn.QueryAsync<Product>("SELECT * FROM Product");
            var dbCities = await salesConn.QueryAsync<City>("SELECT * FROM City");
            var dbCountries = await salesConn.QueryAsync<Country>("SELECT * FROM Country");

            _logger.LogInformation("[ETL] Extracción finalizada. API: {ApiCount}, CSV: {CsvCount}, DB: {DbCount}", 
                apiOrders.Count(), csvOrders.Count(), dbOrders.Count());

            return new ExtractedData(
                apiCustomers, apiOrders, apiOrderDetails,
                csvOrders, csvOrderDetails, csvCustomers,
                dbCustomers, dbOrders, dbOrderDetails,
                dbProducts, dbCities, dbCountries
            );
        }

        private async Task<ServiceResult> CleanDwhTablesAsync()
        {
            try
            {
                _logger.LogInformation("[ETL] Limpiando tablas del Data Warehouse...");
                await _context.FactVentas.ExecuteDeleteAsync();
                await _context.DimClientes.ExecuteDeleteAsync();
                await _context.DimProductos.ExecuteDeleteAsync();
                await _context.DimUbicaciones.ExecuteDeleteAsync();
                await _context.DimTiempos.ExecuteDeleteAsync();
                _logger.LogInformation("[ETL] Limpieza finalizada correctamente.");
                return new ServiceResult { IsSuccess = true, Message = "Tablas limpiadas." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ETL] Error limpiando tablas.");
                return new ServiceResult { IsSuccess = false, Message = $"Error limpiando: {ex.Message}" };
            }
        }

        private async Task LoadDimensionsAsync(ExtractedData data)
        {
            try 
            {
                _logger.LogInformation("[ETL] Cargando dimensiones...");

                var newClientes = data.DbCustomers.Select(c => {
                    var apiMatch = data.ApiCustomers.FirstOrDefault(a => a.Id == c.CustomerId);
                    return new DimCliente {
                        ID_Cliente = c.CustomerId,
                        Nombre_Cliente = $"{c.FirstName} {c.LastName}",
                        Tipo_Cliente = apiMatch?.CustomerType ?? "Regular"
                    };
                }).ToList();
                await _context.DimClientes.AddRangeAsync(newClientes);

                var newProds = data.DbProducts.Select(p => new DimProducto {
                    ID_Producto = p.ProductId,
                    Nombre_Producto = p.ProductName,
                    Categoria = p.CategoryId.ToString(),
                    Precio_Base = p.Price
                }).ToList();
                await _context.DimProductos.AddRangeAsync(newProds);

                var countryMap = data.DbCountries.ToDictionary(c => c.CountryId, c => c.CountryName);
                var newLocs = data.DbCities.Select(city => {
                    string paisName = (city.CountryId.HasValue && countryMap.TryGetValue(city.CountryId.Value, out var name)) ? name : "Unknown";
                    return new DimUbicacion {
                        ID_Ubicacion = city.CityId,
                        Pais = paisName,
                        Region = paisName,
                        Ciudad = city.CityName
                    };
                }).ToList();
                await _context.DimUbicaciones.AddRangeAsync(newLocs);

                await _context.SaveChangesAsync();
                _logger.LogInformation("[ETL] Dimensiones DimCliente ({C1}), DimProducto ({C2}), DimUbicacion ({C3}) cargadas.", 
                    newClientes.Count, newProds.Count, newLocs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ETL] Error en LoadDimensionsAsync");
                throw;
            }
        }

        private async Task<ServiceResult> LoadFactVentasAsync(ExtractedData data)
        {
            try
            {
                _logger.LogInformation("[ETL] Cargando fact...");
                var validClientes = await _context.DimClientes.ToDictionaryAsync(c => c.ID_Cliente);
                var validProds = await _context.DimProductos.ToDictionaryAsync(p => p.ID_Producto);
                var validLocs = await _context.DimUbicaciones.ToDictionaryAsync(u => u.ID_Ubicacion);
                var validLocsByName = await _context.DimUbicaciones.ToDictionaryAsync(u => u.Ciudad.ToLower().Trim());

                var allDates = data.DbOrders.Select(o => o.OrderDate ?? DateTime.Today)
                    .Concat(data.CsvOrders.Select(o => o.OrderDate))
                    .Concat(data.ApiOrders.Select(o => DateTime.TryParse(o.OrderDate, out var d) ? d : DateTime.Today))
                    .Distinct()
                    .ToList();

                await LoadDimTiempoAsync(allDates);
                var validTiempos = await _context.DimTiempos.ToDictionaryAsync(t => t.ID_Tiempo);

                var apiCustLookup = data.ApiCustomers.ToDictionary(c => c.Id);
                var csvCustLookup = data.CsvCustomers.ToDictionary(c => c.CustomerID);

                var factList = new List<FactVenta>();
                int processed = 0, skipped = 0;

                var dbLookup = data.DbOrderDetails.ToLookup(d => d.OrderId);
                foreach (var order in data.DbOrders)
                {
                    int cid = order.CustomerId ?? 0;
                    int uid = data.DbCustomers.FirstOrDefault(c => c.CustomerId == cid)?.CityId ?? 0;
                    int tid = int.Parse((order.OrderDate ?? DateTime.Today).ToString("yyyyMMdd"));

                    if (!validClientes.ContainsKey(cid) || !validTiempos.ContainsKey(tid)) { skipped++; continue; }
                    int? locationId = validLocs.ContainsKey(uid) ? uid : null;

                    var detailedLines = dbLookup[order.OrderId].GroupBy(d => d.ProductId);
                    foreach (var group in detailedLines)
                    {
                        var first = group.First();
                        if (!validProds.ContainsKey(first.ProductId)) { skipped++; continue; }
                        
                        int totalQty = group.Sum(g => g.Quantity);
                        decimal totalPrice = group.Sum(g => g.TotalPrice ?? 0m);

                        factList.Add(CreateFact(order.OrderId, tid, first.ProductId, cid, locationId, totalQty, first.UnitPrice ?? 0m, totalPrice, "DB"));
                        processed++;
                        await CheckBatchSave(factList);
                    }
                }

                var csvLookup = data.CsvOrderDetails.ToLookup(d => d.OrderID);
                foreach (var order in data.CsvOrders)
                {
                    int tid = int.Parse(order.OrderDate.ToString("yyyyMMdd"));
                    if (!validClientes.ContainsKey(order.CustomerID) || !validTiempos.ContainsKey(tid)) { skipped++; continue; }

                    int? locationId = null;
                    if (csvCustLookup.TryGetValue(order.CustomerID, out var customer))
                    {
                        var cityKey = (customer.City ?? "").ToLower().Trim();
                        if (validLocsByName.TryGetValue(cityKey, out var loc)) locationId = loc.ID_Ubicacion;
                    }

                    var detailedLines = csvLookup[order.OrderID].GroupBy(d => d.ProductID);
                    foreach (var group in detailedLines)
                    {
                        var first = group.First();
                        if (!validProds.ContainsKey(first.ProductID)) { skipped++; continue; }

                        int totalQty = group.Sum(g => g.Quantity);
                        decimal totalPrice = group.Sum(g => g.TotalPrice);

                        factList.Add(CreateFact(order.OrderID, tid, first.ProductID, order.CustomerID, locationId, totalQty, 0m, totalPrice, "CSV"));
                        processed++;
                        await CheckBatchSave(factList);
                    }
                }

                var apiLookup = data.ApiOrderDetails.ToLookup(d => d.OrderId);
                foreach (var order in data.ApiOrders)
                {
                    DateTime.TryParse(order.OrderDate, out DateTime date); if (date == default) date = DateTime.Today;
                    int tid = int.Parse(date.ToString("yyyyMMdd"));
                    if (!validClientes.ContainsKey(order.CustomerId) || !validTiempos.ContainsKey(tid)) { skipped++; continue; }

                    int? locationId = null;
                    if (apiCustLookup.TryGetValue(order.CustomerId, out var customer))
                    {
                        var cityKey = (customer.City ?? "").ToLower().Trim();
                        if (validLocsByName.TryGetValue(cityKey, out var loc)) locationId = loc.ID_Ubicacion;
                    }

                    var detailedLines = apiLookup[order.OrderId].GroupBy(d => d.ProductId);
                    foreach (var group in detailedLines)
                    {
                        var first = group.First();
                        if (!validProds.ContainsKey(first.ProductId)) { skipped++; continue; }

                        int totalQty = group.Sum(g => g.Quantity);
                        decimal totalPrice = group.Sum(g => g.TotalPrice);

                        factList.Add(CreateFact(order.OrderId, tid, first.ProductId, order.CustomerId, locationId, totalQty, first.UnitPrice, totalPrice, "API"));
                        processed++;
                        await CheckBatchSave(factList);
                    }
                }

                if (factList.Any())
                {
                    await _context.FactVentas.AddRangeAsync(factList);
                    await _context.SaveChangesAsync();
                }

                var msg = $"Proceso finalizado. [Cargados: {processed}] [Omitidos: {skipped}]";
                _logger.LogInformation("[ETL] {Message}", msg);
                return new ServiceResult { IsSuccess = true, Message = msg };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ETL] Error en LoadFactVentasAsync");
                return new ServiceResult { IsSuccess = false, Message = ex.Message };
            }
        }

        private FactVenta CreateFact(int transId, int timeId, int prodId, int clieId, int? ubicId, int qty, decimal price, decimal total, string source)
        {
            return new FactVenta {
                ID_Transaccion = transId, ID_Tiempo = timeId, ID_Producto = prodId, ID_Cliente = clieId, ID_Ubicacion = ubicId,
                Cantidad = qty, Precio_Unitario = price, Total_Venta = total, Origen_Datos = source
            };
        }

        private async Task CheckBatchSave(List<FactVenta> factList)
        {
            if (factList.Count >= 1000)
            {
                await _context.FactVentas.AddRangeAsync(factList);
                await _context.SaveChangesAsync();
                factList.Clear();
            }
        }

        private async Task LoadDimTiempoAsync(List<DateTime> dates)
        {
            var dateIds = dates.Select(d => int.Parse(d.ToString("yyyyMMdd"))).Distinct().ToList();
            var existingIds = await _context.DimTiempos.Where(t => dateIds.Contains(t.ID_Tiempo)).Select(t => t.ID_Tiempo).ToListAsync();
            var missingIds = dateIds.Except(existingIds).ToHashSet();
            if (!missingIds.Any()) return;

            var missingDates = dates.Where(d => missingIds.Contains(int.Parse(d.ToString("yyyyMMdd")))).DistinctBy(d => int.Parse(d.ToString("yyyyMMdd")))
                .Select(date => new DimTiempo {
                    ID_Tiempo = int.Parse(date.ToString("yyyyMMdd")), Fecha = date, Anio = date.Year, Trimestre = (date.Month - 1) / 3 + 1,
                    Mes = date.Month, Nombre_Mes = date.ToString("MMMM", new CultureInfo("es-ES")), Dia = date.Day
                }).ToList();

            await _context.DimTiempos.AddRangeAsync(missingDates);
            await _context.SaveChangesAsync();
            _logger.LogInformation("[ETL] DimTiempo actualizada con {Count} nuevas fechas.", missingDates.Count);
        }
    }
}
