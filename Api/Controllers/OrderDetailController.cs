using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Data.Interface;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/order-details")]
    public class OrderDetailController : ControllerBase
    {
        private readonly ICsvRepository _csvRepository;

        public OrderDetailController(ICsvRepository csvRepository)
        {
            _csvRepository = csvRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderDetails()
        {
            var details = await _csvRepository.GetOrderDetailsAsync();
            return Ok(details);
        }
    }
}
