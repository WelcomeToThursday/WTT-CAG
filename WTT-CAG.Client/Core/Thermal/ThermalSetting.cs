using BepInEx.Configuration;

namespace WTT_CAG.Client.Core.Thermal;

// A tunable value: the baked Default, or the F12 entry once bound (debug builds only).
public sealed class ThermalSetting<T>
{
    public T Default = default!;

    private ConfigEntry<T>? _entry;

    public T Value => _entry != null ? _entry.Value : Default;

    public void Bind(ConfigFile cfg, string section, string key, string description, AcceptableValueBase? acceptableValues = null)
    {
        _entry = cfg.Bind(section, key, Default, new ConfigDescription(description, acceptableValues));
    }
}
