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

                _logger.LogInformation("[ETL] Iniciando proceso...");
                await handlerService.ProcessVentasDataAsync();
                _logger.LogInformation("[ETL] Proceso finalizado.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el proceso ETL.");
            }
            finally
            {
                _logger.LogInformation("La aplicación se cerrará en 1 minuto...");
                await Task.Delay(1 * 60 * 1000);
                Environment.Exit(0);
            }
        }
    }
}
