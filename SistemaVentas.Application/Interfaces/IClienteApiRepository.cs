using SistemaVentas.Domain.Entities.Api;

namespace SistemaVentas.Application.Interfaces
{
    public interface IClienteApiRepository
    {
        Task<IEnumerable<CustomerAPIDto>> GetCustomersAsync();
    }
}
