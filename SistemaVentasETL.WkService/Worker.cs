using SistemaVentas.Application.Interfaces;

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
            _logger.LogInformation("Proceso ETL iniciado a las: {Time}", DateTimeOffset.Now);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var handlerService = scope.ServiceProvider.GetRequiredService<IVentasHandlerService>();

                _logger.LogInformation("Iniciando pipeline de extracción, transformación y carga...");
                var result = await handlerService.ProcessVentasDataAsync();

                if (result.IsSuccess)
                    _logger.LogInformation("ETL completado exitosamente: {Message}", result.Message);
                else
                    _logger.LogWarning("ETL finalizado con advertencia: {Message}", result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico durante el proceso ETL: {Message}", ex.Message);
            }
            finally
            {
                Environment.Exit(0);
            }
        }
    }
}
