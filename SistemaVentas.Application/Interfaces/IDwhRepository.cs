using SistemaVentas.Application.Result;

namespace SistemaVentas.Application.Interfaces
{
    public interface IDwhRepository
    {
        Task<ServiceResult> LoadVentasDataAsync();
    }
}
