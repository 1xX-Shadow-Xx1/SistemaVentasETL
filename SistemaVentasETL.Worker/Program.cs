using Microsoft.EntityFrameworkCore;
using SistemaVentas.Data.Entities.Api;
using SistemaVentas.Data.Entities.Csv;
using SistemaVentas.Data.Entities.Db;
using SistemaVentas.Data.Interfaces;
using SistemaVentas.Data.Persistence.Api;
using SistemaVentas.Data.Persistence.Context;
using SistemaVentas.Data.Persistence.Csv;
using SistemaVentas.Data.Persistence.Db;
using SistemaVentas.Data.Persistence.Dwh;
using SistemaVentas.Data.Persistence.Staging;
using SistemaVentasETL.Worker;

var builder = Host.CreateApplicationBuilder(args);

// 1. Servicio de guardado temporal (Staging)
builder.Services.AddSingleton<StagingService>();

// 2. Extractor API REST (Configura HttpClient automáticamente)
builder.Services.AddHttpClient<IApiExtractor<ClientApi>, ClienteApiExtractor>();

// 3. Extractores de Archivos y Base de Datos
builder.Services.AddTransient<ICsvExtractor<VentaCsv>, VentaCsvExtractor>();
builder.Services.AddTransient<IDatabaseExtractor<ClienteDb>, ClienteDbExtractor>();

builder.Services.AddDbContext<VentasDwhContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("VentasDB")));

builder.Services.AddTransient<IDwhLoadService, DwhLoadService>();

// 4. El motor principal
builder.Services.AddHostedService<Worker>();    

var host = builder.Build();
host.Run();
