using Microsoft.Extensions.Configuration;
using System.Text.Json;
using SistemaVentas.Data.Persistence.Context;
using SistemaVentas.Data.Entities.Dwh.Dimensions;

namespace SistemaVentas.Data.Persistence.Dwh
{
    public interface IDwhLoadService
    {
        Task LoadDimensionsAsync();
    }

    public class DwhLoadService : IDwhLoadService
    {
        private readonly VentasDwhContext _context;
        private readonly string _stagingBasePath;

        public DwhLoadService(VentasDwhContext context, IConfiguration configuration)
        {
            _context = context;
            _stagingBasePath = configuration["StagingSettings:BasePath"] ?? "StagingArea";
        }

        public async Task LoadDimensionsAsync()
        {
            Console.WriteLine("Iniciando lectura de Staging y carga a SQL Server...");

            await LoadClientesAsync();

            await _context.SaveChangesAsync();

            Console.WriteLine("Datos guardados exitosamente en el Data Warehouse.");
        }

        private async Task LoadClientesAsync()
        {
            var folderPath = Path.Combine(_stagingBasePath, "API");
            if (!Directory.Exists(folderPath)) return;

            var file = new DirectoryInfo(folderPath)
                .GetFiles("Clientes_API_*.json")
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();

            if (file != null)
            {
                var json = await File.ReadAllTextAsync(file.FullName);

                var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var clientesStaging = JsonSerializer.Deserialize<List<DimCliente>>(json, opciones);

                if (clientesStaging != null)
                {
                    foreach (var cliente in clientesStaging)
                    {
                        if (!_context.DimClientes.Any(c => c.ID_Cliente == cliente.ID_Cliente))
                        {
                            _context.DimClientes.Add(cliente);
                        }
                    }
                }
            }
        }
    }
}