using UnityEngine;

namespace WTT_CAG.Client.Core.Thermal;

// One selectable thermal mode on a device: the name its display shows, how the thermal is drawn,
// and any per-mode differences from the device's own settings.
public sealed class ThermalMode(string label, int renderMode, Color? hotColor = null, Color? coldColor = null, bool? heatKeyOnly = null)
{
    // Shown on the device's HUD.
    public readonly string Label = label;

    // ThermalTraceOverlay's _RenderMode: 0 = Trace, 1 = Full, 2 = Outline.
    public readonly int RenderMode = renderMode;

    // Colours for this mode. Null uses the device's own Hot Color and Cold Color.
    public readonly Color? HotColor = hotColor;
    public readonly Color? ColdColor = coldColor;

    // Overrides the device's Heat Only setting for this mode. Null keeps the device's own.
    public readonly bool? HeatKeyOnly = heatKeyOnly;

    // Edges in the device's own colour.
    public static readonly ThermalMode Outline = new("OUTLINE", 2);

    // White on black, whatever colour the device's outline uses.
    public static readonly ThermalMode WhiteHot = new("WHITE HOT", 1, Color.white, Color.black);

    // Black on white. Never heat-keyed: hot objects are the dark ones, so keying on brightness
    // would cut away exactly what the mode is meant to show.
    public static readonly ThermalMode BlackHot = new("BLACK HOT", 1, Color.black, Color.white, heatKeyOnly: false);

    public static readonly ThermalMode Trace = new("TRACE", 0);

    // SkeetIR edge detect: outlines laid over the night vision, so the cold areas stay see-through.
    public static readonly ThermalMode EdgeDetect = new("EDGE DETECT", 2, heatKeyOnly: true);
}
