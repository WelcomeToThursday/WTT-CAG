namespace WTT_CAG.Client.Core.Thermal;

public class ThermalZoneSlot
{
    public readonly ThermalSetting<bool> Enabled = new();
    public readonly ThermalSetting<float> CenterX = new();
    public readonly ThermalSetting<float> CenterY = new();
    public readonly ThermalSetting<float> Radius = new();
    public readonly ThermalSetting<float> Softness = new();
}
