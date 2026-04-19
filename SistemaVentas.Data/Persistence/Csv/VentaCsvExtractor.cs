using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using SistemaVentas.Data.Entities.Csv;
using SistemaVentas.Data.Interfaces;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaVentas.Data.Persistence.Csv
{
    public class VentaCsvExtractor : ICsvExtractor<OrderCsv>
    {
        private readonly IConfiguration _config;

        // Lee la ruta desde el appsettings del Worker
        public string FilePath => _config["CsvSettings:VentasFilePath"] ?? "Datos\\ventas.csv";

        public string SourceType => "CSV";
        public string EntityName => nameof(OrderCsv);
        public string SourceName => FilePath;

        public VentaCsvExtractor(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IEnumerable<OrderCsv>> ExtractAsync()
        {
            if (!File.Exists(FilePath))
                return new List<OrderCsv>();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            };

            using var reader = new StreamReader(FilePath);
            using var csv = new CsvReader(reader, config);

            var records = csv.GetRecords<OrderCsv>().ToList();
            return await Task.FromResult(records);
        }
    }
}
