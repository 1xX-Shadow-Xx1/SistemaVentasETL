using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Data.Interface;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly ICsvRepository _csvRepository;

        public OrderController(ICsvRepository csvRepository)
        {
            _csvRepository = csvRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _csvRepository.GetOrdersAsync();
            return Ok(orders);
        }
    }
}
