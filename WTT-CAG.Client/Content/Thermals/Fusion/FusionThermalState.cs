using Comfort.Common;
using EFT;
using WTT_CAG.Client.Core.Thermal;

namespace WTT_CAG.Client.Content.Thermals.Fusion;

// What the player has switched on the worn goggle with the hotkeys. Not saved: it resets whenever
// the worn device changes.
public static class FusionThermalState
{
    private static int _modeIndex;
    private static GameWorld? _runTimeWorld;

    // How long a fusion goggle's tube has been switched on this raid.
    public static float RunTimeSeconds { get; private set; }

    // Called every frame the tube is on. Starts again from zero in a new raid.
    public static void AddRunTime(float seconds)
    {
        var world = Singleton<GameWorld>.Instance;

        if (world != _runTimeWorld)
        {
            _runTimeWorld = world;
            RunTimeSeconds = 0f;
        }

        RunTimeSeconds += seconds;
    }

    // What the HUD shows for the current mode.
    public static string ModeLabel(ThermalDeviceProfile profile)
    {
        if (!ThermalOn) { return "I2 ONLY"; }

        return RenderMode(profile) switch
        {
            0 => "TRACE",
            1 => "WHITE HOT",
            _ => "OUTLINE"
        };
    }

    // The thermal channel only. The night vision tube and the device overlay are unaffected.
    public static bool ThermalOn { get; private set; } = true;

    public static void Reset()
    {
        ThermalOn = true;
        _modeIndex = 0;
    }

    // Index 0 is the profile's own render mode, so that stays tunable. The rest are its extra modes.
    public static int RenderMode(ThermalDeviceProfile profile)
    {
        if (_modeIndex <= 0 || _modeIndex > profile.ExtraModes.Length)
        {
            return profile.RenderMode.Value;
        }

        return profile.ExtraModes[_modeIndex - 1];
    }

    public static void ToggleThermal()
    {
        var profile = ThermalDeviceProfiles.Active;

        if (profile == null) { return; }

        ThermalOn = !ThermalOn;

        Plugin.logger!.LogInfo($"[WTT-CAG] {profile.Name} thermal {(ThermalOn ? "on" : "off")}.");
    }

    public static void CycleMode()
    {
        var profile = ThermalDeviceProfiles.Active;

        if (profile == null || profile.ExtraModes.Length == 0) { return; }

        _modeIndex = (_modeIndex + 1) % (profile.ExtraModes.Length + 1);

        Plugin.logger!.LogInfo($"[WTT-CAG] {profile.Name} render mode {RenderMode(profile)}.");
    }
}
