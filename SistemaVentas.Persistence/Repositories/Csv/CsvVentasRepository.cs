using Microsoft.Extensions.Configuration;
using SistemaVentas.Application.Interfaces;
using SistemaVentas.Domain.Entities.Csv;

namespace SistemaVentas.Persistence.Repositories.Csv
{
    public class CsvVentasRepository : ICsvVentasRepository
    {
        private readonly IConfiguration _configuration;

        public CsvVentasRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IEnumerable<OrderCsv>> GetVentasAsync()
        {
            var path = _configuration["CsvPaths:Orders"];
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return Enumerable.Empty<OrderCsv>();

            var lines = await File.ReadAllLinesAsync(path);
            var result = new List<OrderCsv>();

            // CSV Columns: OrderID,CustomerID,OrderDate,Status
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 4 && int.TryParse(parts[0], out int orderId))
                {
                    int.TryParse(parts[1], out int customerId);
                    DateTime.TryParse(parts[2], out DateTime orderDate);

                    result.Add(new OrderCsv
                    {
                        OrderID = orderId,
                        CustomerID = customerId,
                        OrderDate = orderDate,
                        Status = parts[3].Trim()
                    });
                }
            }
            return result;
        }
    }
}
