#region

using PrinterDiscoveryService;

#endregion

var builder = Host.CreateApplicationBuilder(args);

builder.AddSeqEndpoint("seq");

builder.AddServiceDefaults();

builder.AddRabbitMQClient("rmq");

builder.Services.AddHostedService<DiscoverWorker>();

var host = builder.Build();
host.Run();
