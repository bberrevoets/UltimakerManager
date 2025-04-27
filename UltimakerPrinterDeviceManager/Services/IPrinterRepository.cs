using UltimakerManager.ServiceDefaults.Models;

namespace UltimakerPrinterDeviceManager.Services;

public interface IPrinterRepository
{
    IEnumerable<UltimakerPrinter> GetAll();
    UltimakerPrinter? GetBySerialNumber(string serialNumber);
    UltimakerPrinter? GetByIdentifier(string identifier);
    Task AddOrUpdateAsync(UltimakerPrinter printer);
    void Save();
}
