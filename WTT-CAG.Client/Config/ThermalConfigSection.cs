using UnityEngine;

using BepInEx.Configuration;
using WTT_CAG.Client.Content.Thermals;
using WTT_CAG.Client.Core.Thermal;

namespace WTT_CAG.Client.Config;

public class ThermalConfigSection : IConfigSection
{
    // While true, every thermal tuning value is exposed in the F12 menu. Set to false for release:
    // nothing below the player settings is bound, and the defaults in code become final. Copy the
    // tuned values out of the .cfg into those defaults first - saved F12 values are ignored once off.
    public static readonly bool TuningEnabled = true;

    private static readonly Color DefaultHotColour = new(0.02f, 1.0f, 0.2f, 1f);
    private static readonly Color DefaultColdColour = new(0.0f, 0.02f, 0.0f, 1f);

    // Player settings - always available.
    public static ConfigEntry<bool>? FusionOverlaysEnabled;

    // Holosun optic tuning.
    public static readonly ThermalSetting<float> HeatFloor = new() { Default = 0.30f };
    public static readonly ThermalSetting<float> HeatGain = new() { Default = 2.5f };
    public static readonly ThermalSetting<float> HeatContrast = new() { Default = 0.6f };

    public static readonly ThermalSetting<float> TraceThreshold = new() { Default = 0.5f };
    public static readonly ThermalSetting<float> TraceSoftness = new() { Default = 0.15f };
    public static readonly ThermalSetting<float> TraceBrightness = new() { Default = 1.2f };
    public static readonly ThermalSetting<float> OutlineIntensity = new() { Default = 3.0f };
    public static readonly ThermalSetting<float> OutlineWidth = new() { Default = 0.005f };
    public static readonly ThermalSetting<float> GlowWidth = new() { Default = 0.004f };
    public static readonly ThermalSetting<float> GlowIntensity = new() { Default = 0.3f };

    public static readonly ThermalSetting<Color> HotColor = new() { Default = DefaultHotColour };
    public static readonly ThermalSetting<Color> ColdColor = new() { Default = DefaultColdColour };

    public static readonly ThermalSetting<int> CompositeMode = new() { Default = 2 };

    // Degrees added to the HUD compass so its north agrees with the map's.
    public static readonly ThermalSetting<float> CompassNorthOffset = new() { Default = 0f };

    public static readonly ThermalSetting<bool> DebugShowRawHeat = new() { Default = false };
    public static readonly ThermalSetting<bool> ZoneDebugFill = new() { Default = false };

    public void Init(ConfigFile cfg)
    {
        FusionOverlaysEnabled = cfg.Bind("Thermal", "Fusion Thermal Overlays", true, new ConfigDescription("Show the thermal overlay on fusion night vision goggles. Turn off for plain night vision."));

        ThermalDeviceProfiles.Init();

        if (!TuningEnabled) { return; }

        HeatFloor.Bind(cfg, "Thermal - Heat", "Heat Floor", "", new AcceptableValueRange<float>(0f, 1f));
        HeatGain.Bind(cfg, "Thermal - Heat", "Heat Gain", "", new AcceptableValueRange<float>(1f, 4f));
        HeatContrast.Bind(cfg, "Thermal - Heat", "Heat Contrast", "", new AcceptableValueRange<float>(0.2f, 3f));

        TraceThreshold.Bind(cfg, "Thermal - Trace Mode", "Trace Threshold", "", new AcceptableValueRange<float>(0f, 1f));
        TraceSoftness.Bind(cfg, "Thermal - Trace Mode", "Trace Softness", "", new AcceptableValueRange<float>(0.01f, 0.5f));
        TraceBrightness.Bind(cfg, "Thermal - Trace Mode", "Trace Brightness", "", new AcceptableValueRange<float>(0f, 2f));
        GlowWidth.Bind(cfg, "Thermal - Trace Mode", "Glow Width", "", new AcceptableValueRange<float>(0f, 0.02f));
        GlowIntensity.Bind(cfg, "Thermal - Trace Mode", "Glow Intensity", "", new AcceptableValueRange<float>(0f, 1f));

        OutlineIntensity.Bind(cfg, "Thermal - Outline Mode", "Outline Intensity", "", new AcceptableValueRange<float>(0f, 5f));
        OutlineWidth.Bind(cfg, "Thermal - Outline Mode", "Outline Width", "", new AcceptableValueRange<float>(0.001f, 0.02f));

        HotColor.Bind(cfg, "Thermal - Colours", "Hot Colour", "");
        ColdColor.Bind(cfg, "Thermal - Colours", "Cold Colour", "");

        CompositeMode.Bind(cfg, "Thermal - Compositing", "Composite Mode", "", new AcceptableValueRange<int>(0, 2));

        CompassNorthOffset.Bind(cfg, "Thermal - HUD", "Compass North Offset", "Degrees added to the HUD compass heading. Adjust until it agrees with the in-game compass.", new AcceptableValueRange<float>(-180f, 180f));

        ZoneDebugFill.Bind(cfg, "Thermal - Debug", "Zone Debug Fill", "");
        DebugShowRawHeat.Bind(cfg, "Thermal - Debug", "Show Raw Heat", "");

        foreach (var profile in ThermalDeviceProfiles.All)
        {
            new ThermalDeviceConfigSection(profile).Init(cfg);
        }
    }
}
