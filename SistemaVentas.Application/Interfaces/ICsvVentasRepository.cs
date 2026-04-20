using SistemaVentas.Domain.Entities.Csv;

namespace SistemaVentas.Application.Interfaces
{
    public interface ICsvVentasRepository
    {
        Task<IEnumerable<OrderCsv>> GetVentasAsync();
        Task<IEnumerable<OrderDetailCsv>> GetOrderDetailsAsync();
        Task<IEnumerable<CustomerCsv>> GetCustomersAsync();
    }
}
