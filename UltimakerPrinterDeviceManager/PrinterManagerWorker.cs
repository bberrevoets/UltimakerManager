#region

using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using UltimakerManager.ServiceDefaults.Models;
using UltimakerPrinterDeviceManager.Services;

#endregion

namespace UltimakerPrinterDeviceManager;

public class PrinterManagerWorker : BackgroundService
{
    private readonly IConnection _connection;
    private readonly ILogger<PrinterManagerWorker> _logger;
    private readonly IPrinterApiClient _printerApiClient;
    private readonly IPrinterRepository _printerRepository;

    private IModel? _channel;
    private EventingBasicConsumer? _consumer;

    public PrinterManagerWorker(ILogger<PrinterManagerWorker> logger, IConnection connection,
        IPrinterRepository printerRepository, IPrinterApiClient printerApiClient)
    {
        _logger = logger;
        _connection = connection;
        _printerRepository = printerRepository;
        _printerApiClient = printerApiClient;
    }

    private async Task ProcessMessageAsync(object? sender, BasicDeliverEventArgs e)
    {
        try
        {
            var json = Encoding.UTF8.GetString(e.Body.ToArray());
            var discoveredPrinter = JsonSerializer.Deserialize<DiscoveredPrinter>(json);

            if (discoveredPrinter == null)
            {
                _logger.LogWarning("Received an invalid printer message");
                _channel?.BasicAck(e.DeliveryTag, false);
                return;
            }

            if (string.IsNullOrEmpty(discoveredPrinter.IPv4Address) &&
                string.IsNullOrEmpty(discoveredPrinter.IPv6Address))
            {
                _logger.LogWarning("Discovered printer is missing IP address");
                _channel?.BasicAck(e.DeliveryTag, false);
                return;
            }

            if (discoveredPrinter.Port == 0 || string.IsNullOrEmpty(discoveredPrinter.Name))
            {
                _logger.LogWarning("Discovered printer is missing port or name");
                _channel?.BasicAck(e.DeliveryTag, false);
                return;
            }

            _logger.LogInformation(
                "Processing discovered printer: {MachineType} - {name} at {ip}:{port}",
                discoveredPrinter.MachineType, discoveredPrinter.Name,
                discoveredPrinter.IPv4Address ?? discoveredPrinter.IPv6Address, discoveredPrinter.Port);

            // Create identifier for this printer
            var identifier = $"{discoveredPrinter.Port}:{discoveredPrinter.Name}";

            // Check if we already know about this printer
            var existingPrinter = _printerRepository.GetByIdentifier(identifier);

            // Create or update our printer record
            var ultimakerPrinter = existingPrinter ?? new UltimakerPrinter
            {
                Port = discoveredPrinter.Port,
                Name = discoveredPrinter.Name,
                FirmwareVersion = discoveredPrinter.FirmwareVersion,
                MachineType = discoveredPrinter.MachineType
            };

            ultimakerPrinter.IPv4Address = !string.IsNullOrEmpty(discoveredPrinter.IPv4Address)
                ? discoveredPrinter.IPv4Address
                : existingPrinter?.IPv4Address;
            ultimakerPrinter.IPv6Address = !string.IsNullOrEmpty(discoveredPrinter.IPv6Address)
                ? discoveredPrinter.IPv6Address
                : existingPrinter?.IPv6Address;

            if (string.IsNullOrEmpty(ultimakerPrinter.IPv4Address) &&
                string.IsNullOrEmpty(ultimakerPrinter.IPv6Address))
            {
                _logger.LogWarning("UltimakerPrinter is missing IP address");
                _channel?.BasicAck(e.DeliveryTag, false);
                return;
            }

            // Query the printer API for more details
            var isOnline = await _printerApiClient.UpdatePrinterStatusAsync(ultimakerPrinter);
            ultimakerPrinter.IsOnline = isOnline;

            // Save the updated printer
            await _printerRepository.AddOrUpdateAsync(ultimakerPrinter);

            _logger.LogInformation(
                "Printer {action}: {name} ({UniqueIdentifier}) is {status}",
                existingPrinter == null ? "added" : "updated",
                ultimakerPrinter.Name,
                ultimakerPrinter.UniqueIdentifier,
                ultimakerPrinter.IsOnline ? "online" : "offline");

            _channel?.BasicAck(e.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing printer message");
            _channel?.BasicAck(e.DeliveryTag, false); // Ack anyway to avoid message buildup
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PrinterManagerWorker starting...");

        // Setup RabbitMQ
        _channel = _connection.CreateModel();
        _channel.QueueDeclare("discoveredPrinterEvents", false, false, false, null);
        _consumer = new EventingBasicConsumer(_channel);

        // Use async delegate for message processing
        _consumer.Received += (sender, e) => { _ = ProcessMessageAsync(sender, e); };

        _channel.BasicConsume("discoveredPrinterEvents", false, _consumer);
        _logger.LogInformation("Started consuming RabbitMQ messages");

        // Start the printer status check timer
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

        try
        {
            // Main service loop
            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait for the next timer tick or cancellation
                await timer.WaitForNextTickAsync(stoppingToken);

                // Check all printers that are currently marked as online
                await CheckPrinterStatusesAsync(stoppingToken);
            }
        }
        finally
        {
            // Cleanup
            // ReSharper disable once EventUnsubscriptionViaAnonymousDelegate
            _consumer.Received -= (sender, e) => { _ = ProcessMessageAsync(sender, e); };
            _channel?.Close();
            _channel?.Dispose();
            _logger.LogInformation("Stopped consuming RabbitMQ messages");
        }
    }

    private async Task CheckPrinterStatusesAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Running periodic printer status check");

        var printers = _printerRepository.GetAll().ToList();
        if (!printers.Any())
        {
            _logger.LogDebug("No printers to check");
            return;
        }

        foreach (var printer in printers)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            if (!printer.IsOnline) continue;

            try
            {
                var isOnline = await _printerApiClient.UpdatePrinterStatusAsync(printer, stoppingToken);
                printer.IsOnline = isOnline;

                // If status changed, log it
                if (!isOnline)
                    _logger.LogDebug(
                        "Printer status changed: {name} at {ip}:{port} is now {status}",
                        printer.Name, printer.IPv4Address ?? printer.IPv6Address, printer.Port,
                        isOnline ? "online" : "offline");

                // Update LastSeen timestamp if online
                if (isOnline)
                    printer.LastSeen = DateTime.UtcNow;

                // Always update in repository to save status changes
                await _printerRepository.AddOrUpdateAsync(printer);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error checking printer status: {name} at {ip}:{port}",
                    printer.Name, printer.IPv4Address ?? printer.IPv6Address, printer.Port);
            }
        }
    }
}
