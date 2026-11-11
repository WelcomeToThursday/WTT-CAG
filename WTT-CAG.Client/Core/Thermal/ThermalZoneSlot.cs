using BepInEx.Configuration;
namespace WTT_CAG.Client.Core.Thermal;

public class ThermalZoneSlot
{
    public ConfigEntry<bool>? Enabled;
    public ConfigEntry<float>? CenterX;
    public ConfigEntry<float>? CenterY;
    public ConfigEntry<float>? Radius;
    public ConfigEntry<float>? Softness;
}
