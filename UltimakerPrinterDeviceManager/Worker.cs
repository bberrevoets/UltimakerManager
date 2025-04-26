#region

using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using UltimakerManager.ServiceDefaults.Models;

#endregion

namespace UltimakerPrinterDeviceManager;

public class Worker : BackgroundService
{
    private readonly IModel _channel;
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger, IConnection connection)
    {
        _logger = logger;
        _channel = connection.CreateModel();

        _channel.QueueDeclare("discoveredPrinterEvents",
            false,
            false,
            false,
            null);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += ProcessMessageAsync;

        _channel.BasicConsume("discoveredPrinterEvents",
            false,
            consumer);
    }

    private void ProcessMessageAsync(object? sender, BasicDeliverEventArgs e)
    {
        var json = Encoding.UTF8.GetString(e.Body.ToArray());
        var printer = JsonSerializer.Deserialize<DiscoveredPrinter>(json);
        if (printer != null)
            _logger.LogInformation(
                "Received printer: Type: {machine_type} - {name} at {ip}:{port} with FirmwareVersion {firmware_version}",
                printer.MachineType, printer.Name,
                printer.IpAddress, printer.Port.ToString(), printer.FirmwareVersion);
        else
            _logger.LogWarning("Received an invalid printer message.");

        _channel.BasicAck(e.DeliveryTag, false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested) await Task.Delay(1000, stoppingToken);
    }
}
