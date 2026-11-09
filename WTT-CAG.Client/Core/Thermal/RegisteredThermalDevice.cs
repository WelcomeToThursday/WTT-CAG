using UnityEngine;
namespace WTT_CAG.Client.Core.Thermal;

public readonly struct RegisteredThermalDevice(Material material, EThermalDeviceType type)
{
    public readonly Material MaterialInstance = material;
    public readonly EThermalDeviceType DeviceType = type;
}
