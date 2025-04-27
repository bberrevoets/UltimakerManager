using UltimakerManager.ServiceDefaults.Models;

namespace UltimakerPrinterDeviceManager.Services;

public interface IPrinterApiClient
{
    Task<bool> UpdatePrinterStatusAsync(UltimakerPrinter printer, CancellationToken cancellationToken = default);
}
