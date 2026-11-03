#region

using Projects;

#endregion

var builder = DistributedApplication.CreateBuilder(args);

var seq = builder.AddSeq("seq")
    .ExcludeFromManifest()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("ACCEPT_EULA", "Y")
    // Aspire 13.6 publishes container ports on 127.0.0.1 only. Its health check calls
    // http://localhost:<port>/health, which Windows tries on ::1 first and times out, so Seq
    // stays Unhealthy and WaitFor(seq) never releases the workers. The DCP proxy (the 13.2
    // behaviour) also listens on ::1.
    .WithEndpoint("http", endpoint => endpoint.IsProxied = true);

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
