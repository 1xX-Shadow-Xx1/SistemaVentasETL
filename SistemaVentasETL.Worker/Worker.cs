using SistemaVentas.Data.Entities.Api;
using SistemaVentas.Data.Entities.Csv;
using SistemaVentas.Data.Entities.Db;
using SistemaVentas.Data.Interfaces;
using SistemaVentas.Data.Persistence.Staging;

namespace SistemaVentasETL.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Proceso ETL (Extracción) iniciado a las: {Time}", DateTimeOffset.Now);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var staging = scope.ServiceProvider.GetRequiredService<StagingService>();

                // Extracción desde CSV
                _logger.LogInformation("Iniciando extracción desde CSV...");
                var csvExtractor = scope.ServiceProvider.GetRequiredService<ICsvExtractor<VentaCsv>>();

                var ventasCsv = await csvExtractor.ExtractAsync();
                await staging.SaveAsync(ventasCsv, "CSV", "Ventas_Crudas");
                _logger.LogInformation("CSV extraído: {Count} registros.", ventasCsv.Count());

                // Extracción desde API REST
                _logger.LogInformation("Iniciando extracción desde API REST...");
                var apiExtractor = scope.ServiceProvider.GetRequiredService<IApiExtractor<ClientApi>>();

                var clientesApi = await apiExtractor.ExtractAsync();
                await staging.SaveAsync(clientesApi, "API", "Clientes_API");
                _logger.LogInformation("API extraída: {Count} registros.", clientesApi.Count());

                // Extracción desde Base de Datos
                _logger.LogInformation("Iniciando extracción desde SQL Server...");

                var dbExtractor = scope.ServiceProvider.GetRequiredService<IDatabaseExtractor<ClienteDb>>();
                var clientesDb = await dbExtractor.ExtractAsync();
                await staging.SaveAsync(clientesDb, "Database", "Clientes_DB");

                _logger.LogInformation("BD extraída: {Count} registros.", clientesDb.Count());
                
                _logger.LogInformation("Iniciando carga de dimensiones al Data Warehouse...");

                var dwhLoadService = scope.ServiceProvider.GetRequiredService<SistemaVentas.Data.Persistence.Dwh.IDwhLoadService>();
                await dwhLoadService.LoadDimensionsAsync();
                _logger.LogInformation("Carga de dimensiones completada exitosamente.");

                _logger.LogInformation("Proceso ETL completo (Extracción y Carga) finalizado exitosamente.");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico durante la extracción: {Message}", ex.Message);
            }
            finally
            {
                Environment.Exit(0);
            }
        }
    }
}
