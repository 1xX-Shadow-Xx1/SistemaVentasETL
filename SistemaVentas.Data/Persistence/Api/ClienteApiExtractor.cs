using Microsoft.Extensions.Configuration;
using SistemaVentas.Data.Entities.Api;
using SistemaVentas.Data.Interfaces;

namespace SistemaVentas.Data.Persistence.Api
{
    public class ClienteApiExtractor : IApiExtractor<ClientApi>
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public int PageSize => int.Parse(_config["ApiSettings:PageSize"] ?? "100");

        public ClienteApiExtractor(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<IEnumerable<ClientApi>> ExtractAsync()
        {
            var baseUrl = _config["ApiSettings:BaseUrl"];
            var url = $"{baseUrl}/users";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<ClientApi>();

            return new List<ClientApi>
            {
                new ClientApi { Id = 999, Name = "Cliente Desde API", CustomerType = "Online" }
            };
        }
    }
}
