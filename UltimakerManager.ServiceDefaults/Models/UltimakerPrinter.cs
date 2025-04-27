namespace UltimakerManager.ServiceDefaults.Models;

public class UltimakerPrinter
{
    public string SerialNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public string FirmwareVersionFromApi { get; set; } = string.Empty;
    public string? IPv4Address { get; set; } = string.Empty;
    public string? IPv6Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public string MachineType { get; set; } = string.Empty;
    public string MachineTypeName { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public DateTime LastSeen { get; set; }

    // Additional printer properties from API
    public float BedTemperature { get; set; }
    public float Head1Temperature { get; set; }
    public float Head2Temperature { get; set; }
    public string Status { get; set; } = string.Empty;
    public float PrintProgress { get; set; }

    // Create a key that uniquely identifies a printer (Port+Name)
    public string UniqueIdentifier => $"{Port}:{Name}";
}
