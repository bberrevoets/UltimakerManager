#region

using System.Text.Json;
using Makaretu.Dns;
using RabbitMQ.Client;
using UltimakerManager.ServiceDefaults.Models;

#endregion

namespace PrinterDiscoveryService;

public class DiscoverWorker : BackgroundService
{
    private readonly IConnection _connection;
    private readonly ILogger<DiscoverWorker> _logger;
    private readonly MulticastService _mdns = new();
    private IModel? _channel;
    private Message? _query;

    public DiscoverWorker(ILogger<DiscoverWorker> logger, IConnection connection)
    {
        _logger = logger;
        _connection = connection;

        SetupMdnsDiscovery();
    }

    private void SetupMdnsDiscovery()
    {
        _channel = _connection.CreateModel();

        _channel.QueueDeclare("discoveredPrinterEvents",
            false,
            false,
            false,
            null);

        _query = new Message();
        _query.Questions.Add(new Question
        {
            Name = "_ultimaker._tcp.local",
            Type = DnsType.PTR
        });

        _mdns.AnswerReceived += OnMdnsAnswerReceived;
    }

    private void OnMdnsAnswerReceived(object? sender, MessageEventArgs e)
    {
        if (e.Message.Answers.Count == 0) return;

        // check if it is an Ultimaker printer
        if (!e.Message.Answers.Any(a => a.Name.ToString().Contains("_ultimaker"))) return;

        var printer = new DiscoveredPrinter();

        foreach (var answer in e.Message.Answers)
        {
            if (answer is TXTRecord txt)
                foreach (var text in txt.Strings)
                {
                    var parts = text.Split('=');
                    if (parts.Length != 2) continue;
                    switch (parts[0])
                    {
                        case "name":
                            printer.Name = parts[1];
                            break;
                        case "firmware_version":
                            printer.FirmwareVersion = parts[1];
                            break;
                        case "machine":
                            printer.MachineType = parts[1];
                            break;
                    }
                }
            else if (answer is SRVRecord srv) printer.Port = srv.Port;

            var ip = e.RemoteEndPoint.Address.ToString();
            _logger.LogDebug("Discovered IP: {ip}", ip);

            if (string.IsNullOrWhiteSpace(ip))
                continue;

            if (ip.Contains(':'))
            {
                if (!string.IsNullOrWhiteSpace(printer.IPv6Address))
                    continue;
                printer.IPv6Address = $"[{ip}]";
                _logger.LogDebug("Assigned IPv6Address: {IPv6Address}", printer.IPv6Address);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(printer.IPv4Address))
                    continue;
                printer.IPv4Address = ip;
                _logger.LogDebug("Assigned IPv4Address: {IPv4Address}", printer.IPv4Address);
            }
        }

        if (string.IsNullOrEmpty(printer.Name) || printer.Port == 0 ||
            (string.IsNullOrWhiteSpace(printer.IPv4Address) && string.IsNullOrWhiteSpace(printer.IPv6Address)))
        {
            _logger.LogWarning(
                "Received invalid printer data: {MachineType}, {name} at {ip}:{port} with FirmwareVersion {Version}",
                printer.MachineType, printer.Name,
                string.IsNullOrWhiteSpace(printer.IPv4Address) ? printer.IPv6Address : printer.IPv4Address,
                printer.Port.ToString(), printer.FirmwareVersion);
            return;
        }

        var body = JsonSerializer.SerializeToUtf8Bytes(printer);
        _channel.BasicPublish("", "discoveredPrinterEvents", null,
            body);

        _logger.LogInformation(
            "Published printer: Type: {MachineType}, {name} at {ip}:{port} with FirmwareVersion {Version}",
            printer.MachineType, printer.Name,
            string.IsNullOrWhiteSpace(printer.IPv4Address) ? printer.IPv6Address : printer.IPv4Address,
            printer.Port.ToString(), printer.FirmwareVersion);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _mdns.Start();

        while (!stoppingToken.IsCancellationRequested)
        {
            _mdns.SendQuery(_query);

            await Task.Delay(30000, stoppingToken);
        }

        _mdns.Stop();
    }
}
