namespace UltimakerManager.ServiceDefaults.Models;

public class DiscoveredPrinter
{
    public string Name { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public string? IPv4Address { get; set; } = string.Empty;
    public string? IPv6Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public string MachineType { get; set; } = string.Empty;
}
