using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Data.Interface;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : ControllerBase
    {
        private readonly ICsvRepository _csvRepository;

        public ProductController(ICsvRepository csvRepository)
        {
            _csvRepository = csvRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _csvRepository.GetProductsAsync();
            return Ok(products);
        }
    }
}
