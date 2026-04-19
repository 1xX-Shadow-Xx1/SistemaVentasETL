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
                {
                    _logger.LogInformation("================================================");
                    _logger.LogInformation("ETL COMPLETADO EXITOSAMENTE");
                    _logger.LogInformation("Mensaje: {Message}", result.Message);
                    _logger.LogInformation("================================================");
                }
                else
                {
                    _logger.LogWarning("ETL finalizado con advertencias: {Message}", result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico durante el proceso ETL: {Message}", ex.Message);
            }
            finally
            {
                _logger.LogInformation("La aplicación se cerrará en 5 minutos...");
                await Task.Delay(5 * 60 * 1000);
                Environment.Exit(0);
            }
        }
    }
}
