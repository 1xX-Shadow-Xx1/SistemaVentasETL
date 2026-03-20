using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using SistemaVentas.Data.Entities.Csv;
using SistemaVentas.Data.Interfaces;
using System.Globalization;

namespace SistemaVentas.Data.Persistence.Csv
{
    public class VentaCsvExtractor : ICsvExtractor<VentaCsv>
    {
        private readonly IConfiguration _config;

        // Lee la ruta desde el appsettings del Worker
        public string FilePath => _config["CsvSettings:VentasFilePath"] ?? "Datos\\ventas.csv";

        public VentaCsvExtractor(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IEnumerable<VentaCsv>> ExtractAsync()
        {
            if (!File.Exists(FilePath))
                return new List<VentaCsv>();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            };

            using var reader = new StreamReader(FilePath);
            using var csv = new CsvReader(reader, config);

            var records = csv.GetRecords<VentaCsv>().ToList();
            return await Task.FromResult(records);
        }
    }
}
