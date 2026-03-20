using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SistemaVentas.Data.Entities.Db;
using SistemaVentas.Data.Interfaces;

namespace SistemaVentas.Data.Persistence.Db
{
    public class ClienteDbExtractor : IDatabaseExtractor<ClienteDb>
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;

        public ClienteDbExtractor(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("VentasDB") ?? "";
        }

        public async Task<IEnumerable<ClienteDb>> ExtractAsync()
        {
            var clientes = new List<ClienteDb>();
            using var connection = new SqlConnection(_connectionString);

            var query = "SELECT ID_Cliente, Nombre_Cliente, Tipo_Cliente FROM Dim_Cliente";

            using var command = new SqlCommand(query, connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                clientes.Add(new ClienteDb
                {
                    ID_Cliente = reader.GetInt32(0),
                    Nombre_Cliente = reader.GetString(1),
                    Tipo_Cliente = reader.GetString(2)
                });
            }
            return clientes;
        }

    }
}
