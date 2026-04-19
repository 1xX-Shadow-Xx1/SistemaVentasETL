using SistemaVentas.Application.Interfaces;
using SistemaVentas.Application.Services;
using SistemaVentas.Persistence.Repositories.Api;
using SistemaVentas.Persistence.Repositories.Csv;
using SistemaVentas.Persistence.Repositories.Dwh;
using SistemaVentasETL.Worker;

var builder = Host.CreateApplicationBuilder(args);

// 1. HttpClient for the mock API extractor
builder.Services.AddHttpClient<IClienteApiRepository, ClienteApiRepository>(client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
    client.BaseAddress = new Uri(baseUrl);
});

// 2. CSV repository (reads from external path defined in appsettings)
builder.Services.AddTransient<ICsvVentasRepository, CsvVentasRepository>();

// 3. DWH Repository with Dapper
builder.Services.AddTransient<IDwhRepository, DwhRepository>();

// 4. Application Service (orchestrator)
builder.Services.AddTransient<IVentasHandlerService, VentasHandlerService>();

// 5. Background Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
