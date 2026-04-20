using SistemaVentas.Application.Interfaces;
using SistemaVentas.Application.Services;
using SistemaVentas.Persistence.Repositories.Api;
using SistemaVentas.Persistence.Repositories.Csv;
using SistemaVentas.Persistence.Repositories.Dwh;
using SistemaVentasETL.Worker;

using SistemaVentas.Persistence.Repositories.Dwh.Context;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<VentasDwhContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DwhDatabase")));

builder.Services.AddHttpClient<IClienteApiRepository, ClienteApiRepository>(client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddTransient<ICsvVentasRepository, CsvVentasRepository>();

builder.Services.AddTransient<IDwhRepository, DwhRepository>();

builder.Services.AddTransient<IVentasHandlerService, VentasHandlerService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
