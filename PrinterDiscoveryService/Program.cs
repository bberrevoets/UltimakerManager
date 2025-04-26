using PrinterDiscoveryService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddRabbitMQClient("rmq");

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
