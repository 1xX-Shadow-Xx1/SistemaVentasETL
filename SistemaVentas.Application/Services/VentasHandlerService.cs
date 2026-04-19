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
            var result = new ServiceResult();
            try
            {
                await _dwhRepository.LoadVentasDataAsync();
                result.Message = "ETL completado exitosamente.";
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = ex.Message;
            }
            return result;
        }
    }
}
