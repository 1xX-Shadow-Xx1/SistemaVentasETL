using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using SistemaVentas.Domain.Entities.Csv;
using SistemaVentas.Application.Interfaces;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaVentas.Persistence.Repositories.Csv
{
    public class VentaCsvExtractor : ICsvExtractor<OrderCsv>
    {
        private readonly IConfiguration _config;

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
