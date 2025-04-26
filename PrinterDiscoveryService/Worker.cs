using RabbitMQ.Client;
using UltimakerManager.ServiceDefaults.Models;

namespace PrinterDiscoveryService;

public class Worker : BackgroundService
{
    private readonly IModel _channel;
    private readonly IConnection _connection;
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger, IConnection connection)
    {
        _logger = logger;
        _connection = connection;
        _channel = _connection.CreateModel();

        _channel.QueueDeclare("discoveredPrinterEvents",
            false,
            false,
            false,
            null);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            var printer = new DiscoveredPrinter
            {
                Name = "TestPrinter",
                IPv4Address = "192.168.1.100",
                Timestamp = DateTime.Now
            };
            
            var body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(printer);
            _channel.BasicPublish(exchange: "", routingKey: "discoveredPrinterEvents", basicProperties: null,
                body: body);
            
            await Task.Delay(1000, stoppingToken);
        }
    }
}
