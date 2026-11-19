using UnityEngine;

namespace WTT_CAG.Client.Core.Thermal;

public sealed class ThermalMode(string label, int renderMode, Color? hotColor = null, Color? coldColor = null, bool? heatKeyOnly = null)
{
    public readonly string Label = label;

    public readonly int RenderMode = renderMode;

    public readonly Color? HotColor = hotColor;
    public readonly Color? ColdColor = coldColor;

    public readonly bool? HeatKeyOnly = heatKeyOnly;

    public static readonly ThermalMode Outline = new("OUTLINE", 2);

    public static readonly ThermalMode WhiteHot = new("WHITE HOT", 1, Color.white, Color.black);

    // Never heat-keyed: the hot objects are the dark ones.
    public static readonly ThermalMode BlackHot = new("BLACK HOT", 1, Color.black, Color.white, heatKeyOnly: false);

    public static readonly ThermalMode Trace = new("TRACE", 0);

    // Heat-keyed so the black backing's opacity comes from Heat Only Background.
    public static readonly ThermalMode EdgeDetect = new("EDGE DETECT", 2, Color.white, Color.black, heatKeyOnly: true);
}
