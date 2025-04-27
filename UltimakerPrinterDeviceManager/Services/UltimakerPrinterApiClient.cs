using System.Text.Json;
using UltimakerManager.ServiceDefaults.Models;
using UltimakerPrinterDeviceManager.Models;

namespace UltimakerPrinterDeviceManager.Services;

public class UltimakerPrinterApiClient : IPrinterApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UltimakerPrinterApiClient> _logger;

    public UltimakerPrinterApiClient(HttpClient httpClient, ILogger<UltimakerPrinterApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> UpdatePrinterStatusAsync(UltimakerPrinter printer,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Set a short timeout for the request
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);

            // call printer API endpoints - adjust on actual Ultimaker API
            var requestUri = $"http://{printer.IPv4Address ?? printer.IPv6Address}:{printer.Port}/api/v1";

            var (data, updatePrinterStatusAsync) = await FetchFirmwareVersionAsync(requestUri, linkedCts);
            if (!updatePrinterStatusAsync || string.IsNullOrEmpty(data))
                return false;
            printer.FirmwareVersionFromApi = data;

            (data, updatePrinterStatusAsync) = await FetchMachineTypeNameAsync(requestUri, linkedCts);
            if (!updatePrinterStatusAsync || string.IsNullOrEmpty(data))
                return false;
            printer.MachineTypeName = data;

            (data, updatePrinterStatusAsync) = await FetchSerialNumberAsync(requestUri, linkedCts);
            if (!updatePrinterStatusAsync || string.IsNullOrEmpty(data))
                return false;
            printer.SerialNumber = data;

            (data, updatePrinterStatusAsync) = await FetchPrinterStatusAsync(requestUri, linkedCts);
            if (!updatePrinterStatusAsync || string.IsNullOrEmpty(data))
                return false;
            printer.Status = data;

            if (printer.Status == "printing")
            {
                var (printProgress, status) = await FetchPrintProgressAsync(requestUri, linkedCts);
                if (!status) return false;
                printer.PrintProgress = printProgress;
            }

            var (printerData, statusCode) = await FetchPrinterDataAsync(requestUri, linkedCts);
            if (!statusCode || printerData == null) return false;
            printer.BedTemperature = printerData.bed.temperature.current;
            printer.Head1Temperature = printerData.heads[0].extruders[0].hotend.temperature.current;
            printer.Head2Temperature = printerData.heads[0].extruders[1].hotend.temperature.current;

            printer.IsOnline = true;
            printer.LastSeen = DateTime.UtcNow;

            _logger.LogInformation("Updated printer status: {name} at {ip}:{port}",
                printer.Name, printer.IPv4Address ?? printer.IPv6Address, printer.Port);

            return true;
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning("Printer not reachable: {name} at {ip}:{port}",
                printer.Name, printer.IPv4Address ?? printer.IPv6Address, printer.Port);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating printer status: {name} at {ip}:{port}",
                printer.Name, printer.IPv4Address ?? printer.IPv6Address, printer.Port);
            return false;
        }
    }

    private async Task<(string? content, bool)> FetchFirmwareVersionAsync(string requestUri,
        CancellationTokenSource linkedCts)
    {
        var response = await _httpClient.GetAsync($"{requestUri}/system/firmware/stable", linkedCts.Token);
        if (!response.IsSuccessStatusCode) return (null, false);

        var content = await response.Content.ReadAsStringAsync(linkedCts.Token);
        return (content.Replace("\"", ""), true);
    }

    private async Task<(PrinterData? data, bool)> FetchPrinterDataAsync(string requestUri,
        CancellationTokenSource linkedCts)
    {
        var response = await _httpClient.GetAsync($"{requestUri}/printer", linkedCts.Token);
        if (!response.IsSuccessStatusCode)
            return (null, false);

        var content = await response.Content.ReadAsStringAsync(linkedCts.Token);
        return (JsonSerializer.Deserialize<PrinterData>(content), true);
    }

    private async Task<(string? content, bool)> FetchMachineTypeNameAsync(string requestUri,
        CancellationTokenSource linkedCts)
    {
        var response = await _httpClient.GetAsync($"{requestUri}/system/variant", linkedCts.Token);
        if (!response.IsSuccessStatusCode) return (null, false);

        var content = await response.Content.ReadAsStringAsync(linkedCts.Token);
        return (content.Replace("\"", ""), true);
    }

    private async Task<(string? content, bool)> FetchSerialNumberAsync(string requestUri,
        CancellationTokenSource linkedCts)
    {
        var response = await _httpClient.GetAsync($"{requestUri}/system/guid", linkedCts.Token);
        if (!response.IsSuccessStatusCode) return (null, false);

        var content = await response.Content.ReadAsStringAsync(linkedCts.Token);
        return (content.Replace("\"", ""), true);
    }

    private async Task<(string? content, bool)> FetchPrinterStatusAsync(string requestUri,
        CancellationTokenSource linkedCts)
    {
        var response = await _httpClient.GetAsync($"{requestUri}/printer/status", linkedCts.Token);
        if (!response.IsSuccessStatusCode) return (null, false);

        var content = await response.Content.ReadAsStringAsync(linkedCts.Token);
        return (content.Replace("\"", ""), true);
    }

    private async Task<(float progress, bool)> FetchPrintProgressAsync(string requestUri,
        CancellationTokenSource linkedCts)
    {
        var response = await _httpClient.GetAsync($"{requestUri}/print_job/progress", linkedCts.Token);
        if (!response.IsSuccessStatusCode)
            return (0, false);

        var content = await response.Content.ReadAsStringAsync(linkedCts.Token);
        using var document = JsonDocument.Parse(content);
        var progress = document.RootElement.GetProperty("progress").GetDouble();
        return ((float)progress, true);
    }
}
