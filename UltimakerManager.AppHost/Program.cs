#region

using Projects;

#endregion

var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("rmq-username", true);
var password = builder.AddParameter("rmq-password", true);

var rmq = builder.AddRabbitMQ("rmq", username, password)
    .WithDataVolume().WithManagementPlugin();

builder.AddProject<PrinterDiscoveryService>("printerdiscoveryservice")
    .WithReference(rmq).WaitFor(rmq);

builder.AddProject<UltimakerPrinterDeviceManager>("ultimakerprinterdevicemanager")
    .WithReference(rmq).WaitFor(rmq);

builder.Build().Run();
