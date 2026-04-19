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
            try
            {
                var result = await _httpClient.GetFromJsonAsync<IEnumerable<CustomerAPIDto>>("/api/users");
                return result ?? Enumerable.Empty<CustomerAPIDto>();
            }
            catch
            {
                return Enumerable.Empty<CustomerAPIDto>();
            }
        }
    }
}
