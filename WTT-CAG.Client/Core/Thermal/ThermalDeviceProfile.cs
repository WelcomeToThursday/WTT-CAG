using UnityEngine;

namespace WTT_CAG.Client.Core.Thermal;

public class ThermalDeviceProfile(string name, string templateId)
{
    public readonly string Name = name;
    public readonly string TemplateId = templateId;

    public readonly ThermalSetting<float> Fov = new();
    public readonly ThermalSetting<int> Resolution = new();
    public readonly ThermalSetting<int> RenderMode = new();
    public readonly ThermalSetting<float> Zoom = new();
    public readonly ThermalSetting<bool> CullLayers = new();
    public readonly ThermalSetting<string> ExcludeLayers = new();

    public const int MaxZones = 4;

    public readonly ThermalSetting<bool> ZoneEnabled = new();
    public readonly ThermalSetting<int> ZoneCount = new();

    // 0 = up to four circles (the Zones), 1 = one rectangle.
    public readonly ThermalSetting<int> ZoneShape = new();

    // The rectangle follows the HUD's Display panel when there is one, so the two always line up.
    public readonly ThermalSetting<bool> RectFromHud = new();

    // Used for the rectangle when there is no HUD, in the HUD's 1920 x 1080 canvas units.
    public readonly ThermalSetting<float> RectWidth = new();
    public readonly ThermalSetting<float> RectHeight = new();
    public readonly ThermalSetting<float> RectOffsetX = new();
    public readonly ThermalSetting<float> RectOffsetY = new();

    // Shrinks the rectangle on all sides, in canvas units, to sit inside a frame drawn on the HUD.
    public readonly ThermalSetting<float> RectInset = new();
    public readonly ThermalSetting<float> RectSoftness = new();
    public readonly ThermalSetting<float> Opacity = new();

    public readonly ThermalZoneSlot[] Zones = [new(), new(), new(), new()];

    // Render modes the mode hotkey steps through after the default one. Empty means the device has a single mode.
    public int[] ExtraModes = [];

    // Name of this device's HUD prefab in the HUD bundle. Empty means no HUD.
    public string HudPrefab = "";

    public readonly ThermalSetting<bool> ClipToTubes = new();
    public readonly ThermalSetting<float> TubeMaskScale = new();
    public readonly ThermalSetting<float> TubeMaskSoftness = new();
    public readonly ThermalSetting<int> TubeMaskChannel = new();

    public readonly ThermalSetting<bool> HeatKeyOnly = new();
    public readonly ThermalSetting<float> HeatKeyThreshold = new();
    public readonly ThermalSetting<float> HeatKeySoftness = new();

    public readonly ThermalSetting<bool> FxNoise = new();
    public readonly ThermalSetting<bool> FxFpsStuck = new();
    public readonly ThermalSetting<bool> FxMotionBlur = new();
    public readonly ThermalSetting<bool> FxGlitch = new();
    public readonly ThermalSetting<bool> FxPixelate = new();
    public readonly ThermalSetting<float> UnsharpRadius = new();
    public readonly ThermalSetting<float> UnsharpBias = new();

    public readonly ThermalSetting<string> OverlayTexture = new();
    public readonly ThermalSetting<float> OverlayOpacity = new();
    public readonly ThermalSetting<Color> OverlayTint = new();

    public readonly ThermalSetting<float> VignetteStrength = new();
    public readonly ThermalSetting<float> VignetteStart = new();
    public readonly ThermalSetting<float> ScintillationStrength = new();
    public readonly ThermalSetting<float> ScintillationScale = new();
    public readonly ThermalSetting<float> ScintillationSpeed = new();

    public readonly ThermalSetting<Color> HotColor = new();
    public readonly ThermalSetting<Color> ColdColor = new();
    public readonly ThermalSetting<float> HeatFloor = new();
    public readonly ThermalSetting<float> HeatGain = new();
    public readonly ThermalSetting<float> HeatPower = new();
    public readonly ThermalSetting<float> TraceThreshold = new();
    public readonly ThermalSetting<float> TraceSoftness = new();
    public readonly ThermalSetting<float> TraceIntensity = new();
    public readonly ThermalSetting<float> OutlineIntensity = new();
    public readonly ThermalSetting<float> OutlineWidth = new();

    private Texture? _resolvedOverlay;
    private string? _resolvedOverlayName;

    public Texture? ResolveOverlayTexture()
    {
        var want = OverlayTexture.Value ?? string.Empty;

        if (want == _resolvedOverlayName)
        {
            return _resolvedOverlay;
        }

        _resolvedOverlayName = want;
        _resolvedOverlay = null;

        if (string.IsNullOrEmpty(want)) return null;

        if (!Plugin.Bundle)
        {
            Plugin.logger!.LogWarning($"[WTT-CAG] {Name}: bundle unavailable, cannot load overlay '{want}'.");
            return null;
        }

        _resolvedOverlay = Plugin.Bundle!.LoadAsset<Texture2D>(want);

        if (!_resolvedOverlay)
        {
            Plugin.logger!.LogWarning($"[WTT-CAG] {Name}: overlay texture '{want}' not found in the bundle. Use the full asset path, e.g. 'Assets/Thermal Testing/Overlay_{Name.Replace(" ", "")}.png'.");
        }
        else
        {
            Plugin.logger!.LogInfo($"[WTT-CAG] {Name}: overlay '{want}' loaded ({_resolvedOverlay!.width}x{_resolvedOverlay.height}).");
        }

        return _resolvedOverlay;
    }
}
