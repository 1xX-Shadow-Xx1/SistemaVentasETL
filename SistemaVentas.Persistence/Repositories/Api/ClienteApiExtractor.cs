using Microsoft.Extensions.Configuration;
using SistemaVentas.Domain.Entities.Api;
using SistemaVentas.Application.Interfaces;

namespace SistemaVentas.Persistence.Repositories.Api
{
    public class ClienteApiExtractor : IApiExtractor<CustomerAPIDto>
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public int PageSize => int.Parse(_config["ApiSettings:PageSize"] ?? "100");

        public string SourceType => "API";
        public string EntityName => nameof(CustomerAPIDto);
        public string SourceName => _config["ApiSettings:BaseUrl"] ?? "Unknown URL";

        public ClienteApiExtractor(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<IEnumerable<CustomerAPIDto>> ExtractAsync()
        {
            var baseUrl = _config["ApiSettings:BaseUrl"];
            var url = $"{baseUrl}/users";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<CustomerAPIDto>();

            return new List<CustomerAPIDto>
            {
                new CustomerAPIDto { Id = 999, Name = "Cliente Desde API", CustomerType = "Online" }
            };
        }
    }
}
