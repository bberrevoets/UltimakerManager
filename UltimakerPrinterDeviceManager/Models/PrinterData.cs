namespace UltimakerPrinterDeviceManager.Models;

public class PrinterData
{
    public Bed bed { get; set; }
    public Beep beep { get; set; }
    public Diagnostics diagnostics { get; set; }
    public Head[] heads { get; set; }
    public Led led { get; set; }
    public Network network { get; set; }
    public string status { get; set; }
    public Validate_Header validate_header { get; set; }
}

public class Bed
{
    public Pre_Heat pre_heat { get; set; }
    public Temperature temperature { get; set; }
    public string type { get; set; }
}

public class Pre_Heat
{
    public bool active { get; set; }
}

public class Temperature
{
    public float current { get; set; }
    public float target { get; set; }
}

public class Beep
{
}

public class Diagnostics
{
}

public class Led
{
    public Blink blink { get; set; }
    public float brightness { get; set; }
    public float hue { get; set; }
    public float saturation { get; set; }
}

public class Blink
{
}

public class Network
{
    public Ethernet ethernet { get; set; }
    public Wifi wifi { get; set; }
    public Wifi_Networks[] wifi_networks { get; set; }
}

public class Ethernet
{
    public bool connected { get; set; }
    public bool enabled { get; set; }
}

public class Wifi
{
    public bool connected { get; set; }
    public bool enabled { get; set; }
    public string mode { get; set; }
    public string ssid { get; set; }
}

public class Wifi_Networks
{
    public string SSID { get; set; }
    public bool connected { get; set; }
    public bool security_required { get; set; }
    public float strength { get; set; }
}

public class Validate_Header
{
}

public class Head
{
    public float acceleration { get; set; }
    public Extruder[] extruders { get; set; }
    public float fan { get; set; }
    public Jerk jerk { get; set; }
    public Max_Speed max_speed { get; set; }
    public Position position { get; set; }
}

public class Jerk
{
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }
}

public class Max_Speed
{
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }
}

public class Position
{
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }
}

public class Extruder
{
    public Active_Material active_material { get; set; }
    public Feeder feeder { get; set; }
    public Hotend hotend { get; set; }
}

public class Active_Material
{
    public string GUID { get; set; }
    public string guid { get; set; }
    public float length_remaining { get; set; }
}

public class Feeder
{
    public float acceleration { get; set; }
    public float jerk { get; set; }
    public float max_speed { get; set; }
}

public class Hotend
{
    public string id { get; set; }
    public Offset offset { get; set; }
    public string serial { get; set; }
    public Statistics statistics { get; set; }
    public Temperature1 temperature { get; set; }
}

public class Offset
{
    public string state { get; set; }
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }
}

public class Statistics
{
    public string last_material_guid { get; set; }
    public float material_extruded { get; set; }
    public float max_temperature_exposed { get; set; }
    public float time_spent_hot { get; set; }
}

public class Temperature1
{
    public float current { get; set; }
    public float target { get; set; }
}
