using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Api.Models;
using Api.Data.Interface;

namespace Api.Data.Repository
{
    public class CsvRepository : ICsvRepository
    {
        private readonly IConfiguration _configuration;

        public CsvRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IEnumerable<CustomerAPI>> GetCustomersAsync()
        {
            var path = _configuration["CsvPaths:Customers"];
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return new List<CustomerAPI>();

            var lines = await File.ReadAllLinesAsync(path);
            var result = new List<CustomerAPI>();
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 3 && int.TryParse(parts[0], out int id))
                {
                    result.Add(new CustomerAPI
                    {
                        Id = id,
                        Name = $"{parts[1]} {parts[2]}".Trim(),
                        CustomerType = "Regular",
                        City = parts.Length > 5 ? parts[5].Trim() : string.Empty,
                        Country = parts.Length > 6 ? parts[6].Trim() : string.Empty
                    });
                }
            }
            return result;
        }

        public async Task<IEnumerable<ProductAPI>> GetProductsAsync()
        {
            var path = _configuration["CsvPaths:Products"];
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return new List<ProductAPI>();

            var lines = await File.ReadAllLinesAsync(path);
            var result = new List<ProductAPI>();
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 5 && int.TryParse(parts[0], out int id))
                {
                    decimal.TryParse(parts[3], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal price);
                    int.TryParse(parts[4], out int stock);

                    result.Add(new ProductAPI
                    {
                        ProductId = id,
                        ProductName = parts[1],
                        Category = parts[2],
                        Price = price,
                        Stock = stock
                    });
                }
            }
            return result;
        }

        public async Task<IEnumerable<OrderAPI>> GetOrdersAsync()
        {
            var path = _configuration["CsvPaths:Orders"];
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return new List<OrderAPI>();

            var lines = await File.ReadAllLinesAsync(path);
            var result = new List<OrderAPI>();
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 4 && int.TryParse(parts[0], out int id))
                {
                    int.TryParse(parts[1], out int customerId);
                    result.Add(new OrderAPI
                    {
                        OrderId = id,
                        CustomerId = customerId,
                        OrderDate = parts[2],
                        Status = parts[3]
                    });
                }
            }
            return result;
        }

        public async Task<IEnumerable<OrderDetailAPI>> GetOrderDetailsAsync()
        {
            var path = _configuration["CsvPaths:OrderDetails"];
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return new List<OrderDetailAPI>();

            var lines = await File.ReadAllLinesAsync(path);
            var result = new List<OrderDetailAPI>();
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 3 && int.TryParse(parts[0], out int orderId))
                {
                    int.TryParse(parts[1], out int productId);
                    int.TryParse(parts[2], out int quantity);
                    
                    decimal unitPrice = 0;
                    decimal totalPrice = 0;

                    if (parts.Length > 4) {
                        decimal.TryParse(parts[3], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out unitPrice);
                        decimal.TryParse(parts[4], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out totalPrice);
                    } else if (parts.Length > 3) {
                        decimal.TryParse(parts[3], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out totalPrice);
                    }

                    result.Add(new OrderDetailAPI
                    {
                        OrderId = orderId,
                        ProductId = productId,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = totalPrice
                    });
                }
            }
            return result;
        }
    }
}
