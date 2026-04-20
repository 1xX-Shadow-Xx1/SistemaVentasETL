using SistemaVentas.Application.Interfaces;
using SistemaVentas.Application.Result;

namespace SistemaVentas.Application.Services
{
    public class VentasHandlerService : IVentasHandlerService
    {
        private readonly IDwhRepository _dwhRepository;

        public VentasHandlerService(IDwhRepository dwhRepository)
        {
            _dwhRepository = dwhRepository;
        }

        public async Task<ServiceResult> ProcessVentasDataAsync()
        {
            return await _dwhRepository.LoadVentasDataAsync();
        }
    }
}
