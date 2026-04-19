using SistemaVentas.Domain.Entities.Api;

namespace SistemaVentas.Application.Interfaces
{
    public interface IClienteApiRepository
    {
        Task<IEnumerable<CustomerAPIDto>> GetCustomersAsync();
        Task<IEnumerable<OrderAPIDto>> GetOrdersAsync();
        Task<IEnumerable<OrderDetailAPIDto>> GetOrderDetailsAsync();
    }
}
