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
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return [];

            var lines = await File.ReadAllLinesAsync(path);
            var result = new List<OrderCsv>();
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 4 && int.TryParse(parts[0], out int orderId))
                {
                    int.TryParse(parts[1], out int customerId);
                    DateTime.TryParse(parts[2], out DateTime orderDate);
                    result.Add(new OrderCsv { OrderID = orderId, CustomerID = customerId, OrderDate = orderDate, Status = parts[3].Trim() });
                }
            }
            return result;
        }

        public async Task<IEnumerable<OrderDetailCsv>> GetOrderDetailsAsync()
        {
            var path = _configuration["CsvPaths:OrderDetails"];
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return [];

            var lines = await File.ReadAllLinesAsync(path);
            var result = new List<OrderDetailCsv>();
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 3 && int.TryParse(parts[0], out int orderId))
                {
                    int.TryParse(parts[1], out int productId);
                    int.TryParse(parts[2], out int quantity);
                    decimal.TryParse(parts.Length > 4 ? parts[4] : (parts.Length > 3 ? parts[3] : "0"),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out decimal totalPrice);
                    result.Add(new OrderDetailCsv { OrderID = orderId, ProductID = productId, Quantity = quantity, TotalPrice = totalPrice });
                }
            }
            return result;
        }

        public async Task<IEnumerable<CustomerCsv>> GetCustomersAsync()
        {
            var path = _configuration["CsvPaths:Customers"];
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return [];

            var lines = await File.ReadAllLinesAsync(path);
            var result = new List<CustomerCsv>();
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 7 && int.TryParse(parts[0], out int id))
                {
                    result.Add(new CustomerCsv
                    {
                        CustomerID = id,
                        FirstName = parts[1],
                        LastName = parts[2],
                        Email = parts[3],
                        Phone = parts[4],
                        City = parts[5].Trim(),
                        Country = parts[6].Trim()
                    });
                }
            }
            return result;
        }
    }
}
