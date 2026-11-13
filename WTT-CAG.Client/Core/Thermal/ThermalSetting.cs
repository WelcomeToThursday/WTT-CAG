using BepInEx.Configuration;

namespace WTT_CAG.Client.Core.Thermal;

// One tunable value. It always has a default set in code, and only reads from the F12 menu when it
// has been bound - so with tuning switched off, nothing is exposed and the defaults are final.
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
