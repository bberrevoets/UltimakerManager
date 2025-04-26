using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("rmq-username", secret: true);
var password = builder.AddParameter("rmq-password", secret: true);

var rmq = builder.AddRabbitMQ("rmq", userName: username, password: password)
    .WithDataVolume().WithManagementPlugin();

builder.AddProject<PrinterDiscoveryService>("printerdiscoveryservice")
    .WithReference(rmq).WaitFor(rmq);

builder.AddProject<UltimakerPrinterDeviceManager>("ultimakerprinterdevicemanager")
    .WithReference(rmq).WaitFor(rmq);

builder.Build().Run();
