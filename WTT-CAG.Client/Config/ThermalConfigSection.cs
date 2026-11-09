using UnityEngine;

using BepInEx.Configuration;
using WTT_CAG.Client.Content.Thermals;

namespace WTT_CAG.Client.Config;

public class ThermalConfigSection : IConfigSection
{
    private static readonly Color DefaultHotColour = new(0.02f, 1.0f, 0.2f, 1f);
    private static readonly Color DefaultColdColour = new(0.0f, 0.02f, 0.0f, 1f);
    
    public static ConfigEntry<float>? HeatFloor;
    public static ConfigEntry<float>? HeatGain;
    public static ConfigEntry<float>? HeatContrast;

    public static ConfigEntry<float>? TraceThreshold;
    public static ConfigEntry<float>? TraceSoftness;
    public static ConfigEntry<float>? TraceBrightness;
    public static ConfigEntry<float>? OutlineIntensity;
    public static ConfigEntry<float>? OutlineWidth;
    public static ConfigEntry<float>? GlowWidth;
    public static ConfigEntry<float>? GlowIntensity;

    public static ConfigEntry<Color>? HotColor;
    public static ConfigEntry<Color>? ColdColor;

    public static ConfigEntry<int>?   CompositeMode;

    public static ConfigEntry<bool>?  DebugShowRawHeat;
    public static ConfigEntry<bool>?  ZoneDebugFill;
    
    public void Init(ConfigFile cfg)
    {
        HeatFloor = cfg.Bind("Thermal - Heat", "Heat Floor", 0.30f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 1f)));
        HeatGain = cfg.Bind("Thermal - Heat", "Heat Gain", 2.5f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 4f)));
        HeatContrast = cfg.Bind("Thermal - Heat", "Heat Contrast", 0.6f, new ConfigDescription("", new AcceptableValueRange<float>(0.2f, 3f)));

        TraceThreshold = cfg.Bind("Thermal - Trace Mode", "Trace Threshold", 0.5f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 1f)));
        TraceSoftness = cfg.Bind("Thermal - Trace Mode", "Trace Softness", 0.15f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 0.5f)));
        TraceBrightness = cfg.Bind("Thermal - Trace Mode", "Trace Brightness", 1.2f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 2f)));
        GlowWidth = cfg.Bind("Thermal - Trace Mode", "Glow Width", 0.004f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 0.02f)));
        GlowIntensity = cfg.Bind("Thermal - Trace Mode", "Glow Intensity", 0.3f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 1f)));
        
        OutlineIntensity = cfg.Bind("Thermal - Outline Mode", "Outline Intensity", 3.0f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 5f)));
        OutlineWidth = cfg.Bind("Thermal - Outline Mode", "Outline Width", 0.005f, new ConfigDescription("", new AcceptableValueRange<float>(0.001f, 0.02f)));

        HotColor = cfg.Bind("Thermal - Colours", "Hot Colour", DefaultHotColour, new ConfigDescription(""));
        ColdColor = cfg.Bind("Thermal - Colours", "Cold Colour", DefaultColdColour, new ConfigDescription(""));

        CompositeMode = cfg.Bind("Thermal - Compositing", "Composite Mode", 2, new ConfigDescription("", new AcceptableValueRange<int>(0, 2)));

        ZoneDebugFill = cfg.Bind("Thermal - Debug", "Zone Debug Fill", false, new ConfigDescription(""));
        DebugShowRawHeat = cfg.Bind("Thermal - Debug", "Show Raw Heat", false, new ConfigDescription(""));
        
        ThermalDeviceProfiles.Init(cfg);
    }
}
