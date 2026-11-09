using BepInEx.Configuration;
using UnityEngine;

namespace WTT_CAG.Client.Core.Thermal;

public class ThermalDeviceProfile(string name, string templateId)
{
    public readonly string Name = name;
    public readonly string TemplateId = templateId;

    public ConfigEntry<float>? Fov;
    public ConfigEntry<int>? Resolution;
    public ConfigEntry<int>? RenderMode;
    public ConfigEntry<float>? Zoom;
    public ConfigEntry<bool>? CullLayers;
    public ConfigEntry<string>? ExcludeLayers;

    public const int MaxZones = 4;

    public ConfigEntry<bool>? ZoneEnabled;
    public ConfigEntry<int>? ZoneCount;
    public ConfigEntry<float>? Opacity;

    public readonly ThermalZoneSlot[] Zones = new ThermalZoneSlot[MaxZones];

    public ConfigEntry<bool>? ClipToTubes;
    public ConfigEntry<float>? TubeMaskScale;
    public ConfigEntry<float>? TubeMaskSoftness;
    public ConfigEntry<int>? TubeMaskChannel;
    public ConfigEntry<bool>? TubeMaskInvert;

    public ConfigEntry<bool>? HeatKeyOnly;
    public ConfigEntry<float>? HeatKeyThreshold;
    public ConfigEntry<float>? HeatKeySoftness;

    public ConfigEntry<bool>? FxNoise;
    public ConfigEntry<bool>? FxFpsStuck;
    public ConfigEntry<bool>? FxMotionBlur;
    public ConfigEntry<bool>? FxGlitch;
    public ConfigEntry<bool>? FxPixelate;
    public ConfigEntry<float>? UnsharpRadius;
    public ConfigEntry<float>? UnsharpBias;

    public ConfigEntry<string>? OverlayTexture;
    public ConfigEntry<float>? OverlayOpacity;
    public ConfigEntry<Color>? OverlayTint;

    public ConfigEntry<float>? VignetteStrength;
    public ConfigEntry<float>? VignetteStart;
    public ConfigEntry<float>? ScintillationStrength;
    public ConfigEntry<float>? ScintillationScale;
    public ConfigEntry<float>? ScintillationSpeed;

    public ConfigEntry<Color>? HotColor;
    public ConfigEntry<Color>? ColdColor;
    public ConfigEntry<float>? HeatFloor;
    public ConfigEntry<float>? HeatGain;
    public ConfigEntry<float>? HeatPower;
    public ConfigEntry<float>? TraceThreshold;
    public ConfigEntry<float>? TraceSoftness;
    public ConfigEntry<float>? TraceIntensity;
    public ConfigEntry<float>? OutlineIntensity;
    public ConfigEntry<float>? OutlineWidth;

    private Texture? _resolvedOverlay;
    private string? _resolvedOverlayName;
    private bool _loggedOverlayFailure;

    public Texture? ResolveOverlayTexture()
    {
        var want = OverlayTexture?.Value ?? string.Empty;

        if (want == _resolvedOverlayName)
        {
            return _resolvedOverlay;
        }

        _resolvedOverlayName = want;
        _resolvedOverlay = null;
        _loggedOverlayFailure = false;

        if (string.IsNullOrEmpty(want)) return null;

        if (!Plugin.Bundle)
        {
            Plugin.logger!.LogWarning($"[GoggleProfiles] {Name}: bundle unavailable, cannot load overlay '{want}'.");
            return null;
        }

        _resolvedOverlay = Plugin.Bundle!.LoadAsset<Texture2D>(want);

        if (!_resolvedOverlay && !_loggedOverlayFailure)
        {
            _loggedOverlayFailure = true;
            Plugin.logger!.LogWarning($"[GoggleProfiles] {Name}: overlay texture '{want}' not found in the bundle. Use the full asset path, e.g. 'Assets/Thermal Testing/Overlay_{Name.Replace(" ", "")}.png'.");
        }
        else if (_resolvedOverlay)
        {
            Plugin.logger!.LogInfo($"[GoggleProfiles] {Name}: overlay '{want}' loaded ({_resolvedOverlay.width}x{_resolvedOverlay.height}).");
        }

        return _resolvedOverlay;
    }
}
