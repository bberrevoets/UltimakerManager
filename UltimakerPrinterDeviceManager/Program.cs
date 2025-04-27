#region

using UltimakerPrinterDeviceManager;
using UltimakerPrinterDeviceManager.Services;

#endregion

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddRabbitMQClient("rmq");

builder.Services.AddSingleton<IPrinterRepository, PrinterRepository>();
builder.Services.AddHttpClient<IPrinterApiClient, UltimakerPrinterApiClient>();

builder.Services.AddHostedService<PrinterManagerWorker>();

var host = builder.Build();
host.Run();
