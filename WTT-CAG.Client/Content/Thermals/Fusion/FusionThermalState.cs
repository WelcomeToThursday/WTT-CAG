using System.Collections.Generic;
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

    // How long each goggle's tube has been switched on this raid, kept per device so one goggle's
    // hours never show on another's display.
    private static readonly Dictionary<ThermalDeviceProfile, float> RunTimes = [];

    public static float RunTimeSeconds(ThermalDeviceProfile profile)
    {
        return RunTimes.TryGetValue(profile, out var seconds) ? seconds : 0f;
    }

    // Called every frame the worn goggle's tube is on. Every device starts again from zero in a new raid.
    public static void AddRunTime(ThermalDeviceProfile profile, float seconds)
    {
        var world = Singleton<GameWorld>.Instance;

        if (world != _runTimeWorld)
        {
            _runTimeWorld = world;
            RunTimes.Clear();
        }

        RunTimes[profile] = RunTimeSeconds(profile) + seconds;
    }

    // The thermal channel only. The night vision tube and a fused goggle's HUD are unaffected.
    public static bool ThermalOn { get; private set; } = true;

    // A newly worn device starts with thermal on, in its first mode.
    public static void Reset()
    {
        ThermalOn = true;
        _modeIndex = 0;
    }

    // What the HUD shows for the current mode.
    public static string ModeLabel(ThermalDeviceProfile profile)
    {
        return ThermalOn ? CurrentMode(profile).Label : "I2 ONLY";
    }

    // The mode the worn device is in. It starts in its first mode.
    public static ThermalMode CurrentMode(ThermalDeviceProfile profile)
    {
        var modes = profile.Modes;

        return modes[_modeIndex >= 0 && _modeIndex < modes.Length ? _modeIndex : 0];
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

        if (profile == null || profile.Modes.Length <= 1) { return; }

        _modeIndex = (_modeIndex + 1) % profile.Modes.Length;

        Plugin.logger!.LogInfo($"[WTT-CAG] {profile.Name} mode: {CurrentMode(profile).Label}.");
    }
}
