using System.Collections.Generic;
using System.Globalization;
using Comfort.Common;
using EFT;
using WTT_CAG.Client.Core.Thermal;
using WTT_CAG.Client.Core;

namespace WTT_CAG.Client.Content.Thermals.Fusion;

// Hotkey state for the worn goggle. Resets when the worn device changes; runtime resets each raid.
public static class FusionThermalState
{
    private static int _modeIndex;
    private static int _zoomIndex;
    private static GameWorld? _runTimeWorld;

    private static readonly Dictionary<ThermalDeviceProfile, float> RunTimes = [];

    public static float RunTimeSeconds(ThermalDeviceProfile profile)
    {
        return RunTimes.TryGetValue(profile, out var seconds) ? seconds : 0f;
    }

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

    public static bool ThermalOn { get; private set; } = true;

    public static void Reset()
    {
        ThermalOn = true;
        _modeIndex = 0;
        _zoomIndex = 0;
    }

    public static string ModeLabel(ThermalDeviceProfile profile)
    {
        return ThermalOn ? CurrentMode(profile).Label : "I2 ONLY";
    }

    public static ThermalMode CurrentMode(ThermalDeviceProfile profile)
    {
        var modes = profile.Modes;

        return modes[_modeIndex >= 0 && _modeIndex < modes.Length ? _modeIndex : 0];
    }

    // Steps through the modes; with OffInCycle the step after the last mode is thermal off.
    public static float ZoomFactor(ThermalDeviceProfile profile)
    {
        var levels = profile.ZoomLevels;

        return levels[_zoomIndex >= 0 && _zoomIndex < levels.Length ? _zoomIndex : 0];
    }

    public static string ZoomLabel(ThermalDeviceProfile profile)
    {
        return $"{ZoomFactor(profile).ToString("0.##", CultureInfo.InvariantCulture)}X";
    }

    public static void CycleZoom()
    {
        var profile = ThermalDeviceProfiles.Active;

        if (profile == null || profile.ZoomLevels.Length <= 1) { return; }

        _zoomIndex = (_zoomIndex + 1) % profile.ZoomLevels.Length;

        CagLog.Info($"[WTT-CAG] {profile.Name} zoom: {ZoomLabel(profile)}.");
    }

    public static void CycleMode()
    {
        var profile = ThermalDeviceProfiles.Active;

        if (profile == null) { return; }

        if (!ThermalOn)
        {
            ThermalOn = true;
            _modeIndex = 0;
        }
        else if (_modeIndex + 1 < profile.Modes.Length)
        {
            _modeIndex++;
        }
        else if (profile.OffInCycle)
        {
            ThermalOn = false;
        }
        else
        {
            _modeIndex = 0;
        }

        CagLog.Info($"[WTT-CAG] {profile.Name} mode: {ModeLabel(profile)}.");
    }
}
