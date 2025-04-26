namespace UltimakerManager.ServiceDefaults.Models;

public class DiscoveredPrinter
{
    public string Name { get; set; } = string.Empty;
    public string IPv4Address { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
