using SistemaVentas.Application.Result;

namespace SistemaVentas.Application.Interfaces
{
    public interface IVentasHandlerService
    {
        Task<ServiceResult> ProcessVentasDataAsync();
    }
}
