using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace SistemaVentas.Data.Persistence.Staging
{
    public class StagingService 
    {
        private readonly string _stagingBasePath;

        public StagingService(IConfiguration configuration)
        {
            _stagingBasePath = configuration["StagingSettings:BasePath"] ?? "StagingArea";
        }

        public async Task SaveAsync<T>(IEnumerable<T> data, string source, string fileName)
        {
            var folderPath = Path.Combine(_stagingBasePath, source);
            Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, $"{fileName}_{DateTime.Now:yyyyMMdd_HHmmss}.json");

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(data, jsonOptions);

            await File.WriteAllTextAsync(filePath, json);
            Console.WriteLine($"Staging guardado en: {filePath}");
        }
    }
}
