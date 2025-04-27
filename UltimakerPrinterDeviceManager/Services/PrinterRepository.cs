using System.Text.Json;
using UltimakerManager.ServiceDefaults.Models;

namespace UltimakerPrinterDeviceManager.Services;

public class PrinterRepository : IPrinterRepository
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<PrinterRepository> _logger;
    private readonly Dictionary<string, UltimakerPrinter> _printers = new();

    public PrinterRepository(ILogger<PrinterRepository> logger, IConfiguration configuration)
    {
        _logger = logger;

        // Get Storage path from configuration or use default
        var dataFolder = configuration["PrinterRepository:DataPath"] ??
                         Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                             "UltimakerPrinterDeviceManager");

        if (!Directory.Exists(dataFolder))
            Directory.CreateDirectory(dataFolder);

        _filePath = Path.Combine(dataFolder, "printers.json");

        LoadPrinters();
    }

    public IEnumerable<UltimakerPrinter> GetAll()
    {
        return _printers.Values;
    }

    public UltimakerPrinter? GetBySerialNumber(string serialNumber)
    {
        return _printers.Values.FirstOrDefault(p => p.SerialNumber == serialNumber);
    }

    public UltimakerPrinter? GetByIdentifier(string identifier)
    {
        return _printers.Values.FirstOrDefault(p => p.UniqueIdentifier == identifier);
    }

    public async Task AddOrUpdateAsync(UltimakerPrinter printer)
    {
        await _lock.WaitAsync();
        try
        {
            _printers[printer.UniqueIdentifier] = printer;
            Save();
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_printers.Values.ToList());
            File.WriteAllText(_filePath, json);
            _logger.LogDebug("Saved {count} printers to storage", _printers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving printers to storage");
        }
    }

    private void LoadPrinters()
    {
        try
        {
            if (!File.Exists(_filePath))
                return;

            var json = File.ReadAllText(_filePath);
            var printers = JsonSerializer.Deserialize<List<UltimakerPrinter>>(json) ?? [];

            // Mark all as offline initially
            foreach (var printer in printers)
            {
                printer.IsOnline = false;
                _printers[printer.UniqueIdentifier] = printer;
            }

            _logger.LogInformation("Loaded {count} printers from storage", _printers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading printers from storage");
        }
    }
}
