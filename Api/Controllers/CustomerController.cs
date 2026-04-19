using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Data.Interface;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class CustomerController : ControllerBase
    {
        private readonly ICsvRepository _csvRepository;

        public CustomerController(ICsvRepository csvRepository)
        {
            _csvRepository = csvRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers()
        {
            var customers = await _csvRepository.GetCustomersAsync();
            return Ok(customers);
        }
    }
}
