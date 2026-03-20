using SistemaVentasETL.Worker;
using SistemaVentas.Data.Interfaces;
using SistemaVentas.Data.Persistence.Api;
using SistemaVentas.Data.Persistence.Csv;
using SistemaVentas.Data.Persistence.Db;
using SistemaVentas.Data.Persistence.Staging;
using SistemaVentas.Data.Entities.Api;
using SistemaVentas.Data.Entities.Csv;
using SistemaVentas.Data.Entities.Db;

var builder = Host.CreateApplicationBuilder(args);

// 1. Servicio de guardado temporal (Staging)
builder.Services.AddSingleton<StagingService>();

// 2. Extractor API REST (Configura HttpClient automáticamente)
builder.Services.AddHttpClient<IApiExtractor<ClientApi>, ClienteApiExtractor>();

// 3. Extractores de Archivos y Base de Datos
builder.Services.AddTransient<ICsvExtractor<VentaCsv>, VentaCsvExtractor>();
builder.Services.AddTransient<IDatabaseExtractor<ClienteDb>, ClienteDbExtractor>();

// 4. El motor principal
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
