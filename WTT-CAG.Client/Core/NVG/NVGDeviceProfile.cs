using BepInEx.Configuration;
using EFT.InventoryLogic;
using UnityEngine;
using WTT_CAG.Client.Config;
namespace WTT_CAG.Client.Core;

public class NVGDeviceProfile
{
    public readonly string PrefabNameContains;
    public readonly string ChildNVGName;
    public readonly string LeftPodPath;
    public readonly string RightPodPath;

    public readonly NightVisionComponent.EMask? SinglePodMask;

    private readonly Quaternion _defLeftOn, _defLeftOff, _defRightOn, _defRightOff;

    private readonly ConfigEntry<float>[]? _leftOn = new ConfigEntry<float>[4];
    private readonly ConfigEntry<float>[]? _leftOff = new ConfigEntry<float>[4];
    private readonly ConfigEntry<float>[]? _rightOn = new ConfigEntry<float>[4];
    private readonly ConfigEntry<float>[]? _rightOff = new ConfigEntry<float>[4];

    public Quaternion LeftOn => FromConfigEntry(_leftOn!);
    public Quaternion LeftOff => FromConfigEntry(_leftOff!);
    public Quaternion RightOn => FromConfigEntry(_rightOn!);
    public Quaternion RightOff => FromConfigEntry(_rightOff!);

    private static Quaternion FromConfigEntry(ConfigEntry<float>[] entry) => new(entry[0].Value, entry[1].Value, entry[2].Value, entry[3].Value);

    public static void SetQuaternion(ConfigEntry<float>[] entry, Quaternion q)
    {
        entry[0].Value = q.x;
        entry[1].Value = q.y;
        entry[2].Value = q.z;
        entry[3].Value = q.w;
    }

    public void ResetToDefaults()
    {
        SetQuaternion(_leftOn!, _defLeftOn);
        SetQuaternion(_leftOff!, _defLeftOff);
        SetQuaternion(_rightOn!, _defRightOn);
        SetQuaternion(_rightOff, _defRightOff);
    }

    public NVGDeviceProfile(NVGDeviceDefinition device, ConfigFile cfg)
    {
        PrefabNameContains = device.PrefabNameContains;
        ChildNVGName = device.ChildNvgName;
        LeftPodPath = device.LeftPodPath;
        RightPodPath = device.RightPodPath;
        SinglePodMask = device.SinglePodMask;

        _defLeftOn = device.DefaultLeftOn;
        _defLeftOff = device.DefaultLeftOff;
        _defRightOn = device.DefaultRightOn;
        _defRightOff = device.DefaultRightOff;
        var sec = $"Device: {device.PrefabNameContains}";

        string[] comp = ["X", "Y", "Z", "W"];

        Bind(_leftOn!, "Left Pod On", device.DefaultLeftOn);
        Bind(_leftOff!, "Left Pod Off", device.DefaultLeftOff);
        Bind(_rightOn!, "Right Pod On", device.DefaultRightOn);
        Bind(_rightOff!, "Right Pod Off", device.DefaultRightOff);
        return;

        void Bind(ConfigEntry<float>[] arr, string label, Quaternion q)
        {
            float[] vals = [q.x, q.y, q.z, q.w];

            for (var i = 0; i < 4; i++)
            {
                var description = new ConfigDescription($"{label} quaternion {comp[i]} — copy from Unity Inspector", null, new CAGConfigurationManagerAttributes
                {
                    IsAdvanced = true
                });
                arr[i] = cfg.Bind(sec, $"{label} {comp[i]}", vals[i], description);
            }
        }
    }
}
