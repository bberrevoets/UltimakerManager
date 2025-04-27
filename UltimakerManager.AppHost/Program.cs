#region

using Projects;

#endregion

var builder = DistributedApplication.CreateBuilder(args);

var seq = builder.AddSeq("seq")
    .ExcludeFromManifest()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("ACCEPT_EULA", "Y");

var username = builder.AddParameter("rmq-username", true);
var password = builder.AddParameter("rmq-password", true);

var rmq = builder.AddRabbitMQ("rmq", username, password)
    .WithDataVolume().WithManagementPlugin();

builder.AddProject<PrinterDiscoveryService>("printerdiscoveryservice")
    .WithReference(rmq)
    .WithReference(seq)
    .WaitFor(seq)
    .WaitFor(rmq);

builder.AddProject<UltimakerPrinterDeviceManager>("ultimakerprinterdevicemanager")
    .WithReference(rmq)
    .WithReference(seq)
    .WaitFor(seq)
    .WaitFor(rmq);

builder.Build().Run();
