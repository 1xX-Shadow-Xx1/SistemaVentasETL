using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using SistemaVentas.Application.Interfaces;
using SistemaVentas.Domain.Entities.Api;

namespace SistemaVentas.Persistence.Repositories.Api
{
    public class ClienteApiRepository : IClienteApiRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ClienteApiRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<IEnumerable<CustomerAPIDto>> GetCustomersAsync()
        {
            try { return await _httpClient.GetFromJsonAsync<IEnumerable<CustomerAPIDto>>("/api/users") ?? []; }
            catch { return []; }
        }

        public async Task<IEnumerable<OrderAPIDto>> GetOrdersAsync()
        {
            try { return await _httpClient.GetFromJsonAsync<IEnumerable<OrderAPIDto>>("/api/orders") ?? []; }
            catch { return []; }
        }

        public async Task<IEnumerable<OrderDetailAPIDto>> GetOrderDetailsAsync()
        {
            try { return await _httpClient.GetFromJsonAsync<IEnumerable<OrderDetailAPIDto>>("/api/order-details") ?? []; }
            catch { return []; }
        }
    }
}
