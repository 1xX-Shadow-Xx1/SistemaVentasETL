using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Models;

namespace Api.Data.Interface
{
    public interface ICsvRepository
    {
        Task<IEnumerable<CustomerAPI>> GetCustomersAsync();
        Task<IEnumerable<ProductAPI>> GetProductsAsync();
        Task<IEnumerable<OrderAPI>> GetOrdersAsync();
        Task<IEnumerable<OrderDetailAPI>> GetOrderDetailsAsync();
    }
}
