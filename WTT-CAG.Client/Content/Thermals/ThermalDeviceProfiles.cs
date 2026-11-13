using System.Collections.Generic;
using Comfort.Common;
using EFT;
using UnityEngine;
using WTT_CAG.Client.Config;
using WTT_CAG.Client.Content.Thermals.Fusion;
using WTT_CAG.Client.Core.Thermal;

namespace WTT_CAG.Client.Content.Thermals;

public static class ThermalDeviceProfiles
{
    private static readonly List<ThermalDeviceProfile> ProfileCache = [];
    public static IReadOnlyList<ThermalDeviceProfile> All => ProfileCache;
    public static ThermalDeviceProfile? Active;
    private static string? _lastTemplateId;

    public static void Init()
    {
        ProfileCache.Clear();
        Active = null;
        _lastTemplateId = null;

        Register("BNVG-Fused", "6a2dcf2e11cf8134015c0563", hot: new Color(1.0f, 0.357f, 0.0f, 1f), renderMode: 2, zoom: 1.5f, radius: 0.35f, opacity: 0.72f, heatFloor: 0f, traceThreshold: 0.30f, outlineIntensity: 1.08f, extraModes: [1]);
        Register("ECOTI-PVS-31", "6aa58d6c394c0807cb0af580", renderMode: 2, heatKeyOnly: true, zoneCount: 2, zoneCentersX: [0.37f, 0.63f], zoneCentersY: [0.5f, 0.5f], zoneEnabled: [true, true], radius: 0.26f, softness: 0.06f, extraModes: [1]);
        Register("F-PANO Fused", "6aece3759b55c16086112d9e", renderMode: 2, heatKeyOnly: true, zoneCount: 4, zoneCentersX: [0.22f, 0.41f, 0.59f, 0.78f], zoneCentersY: [0.48f, 0.50f, 0.50f, 0.48f], zoneEnabled: [true, true, true, true], radius: 0.20f, softness: 0.07f, extraModes: [1], hud: "FPanoHud", zoneShape: 1, clipToTubes: false);
        Register("Breach / PVS-14", "6aa58e595da66037460af585", renderMode: 1, heatKeyOnly: false, hot: new Color(1f, 1f, 1f, 1f), cold: new Color(0f, 0f, 0f, 1f), zoneCount: 2, zoneCentersX: [0.37f, 0.63f], zoneCentersY: [0.5f, 0.5f], zoneEnabled: [true, false], radius: 0.26f, softness: 0.10f, opacity: 0.95f);

        Plugin.logger!.LogInfo($"[WTT-CAG] Registered {ProfileCache.Count} thermal device profile(s).");
    }

    public static void RefreshActive()
    {
        var player = Singleton<GameWorld>.Instance?.MainPlayer;

        // With the player's overlay setting off, no device is ever active and the goggles stay stock.
        var overlaysEnabled = ThermalConfigSection.FusionOverlaysEnabled?.Value ?? true;
        var templateId = overlaysEnabled ? player?.NightVisionObserver?.Component?.Item?.StringTemplateId : null;

        // Only search when the worn device actually changes.
        if (templateId == _lastTemplateId) { return; }

        _lastTemplateId = templateId;

        // A newly worn device starts with thermal on, in its default mode.
        FusionThermalState.Reset();

        var previous = Active;
        Active = templateId == null ? null : ProfileCache.Find(profile => profile.TemplateId == templateId);

        if (Active != previous)
        {
            Plugin.logger!.LogInfo($"[WTT-CAG] Active thermal device: {Active?.Name ?? "none"}");
        }
    }

    private static void Register(
        string name,
        string templateId,
        Color? hot = null,
        Color? cold = null,
        float fov = 40f,
        int resolution = 640,
        int renderMode = 2,
        float zoom = 1.5f,
        float radius = 0.35f,
        float softness = 0.05f,
        float centerX = 0.5f,
        float centerY = 0.5f,
        float opacity = 1f,
        int zoneCount = 1,
        float[]? zoneCentersX = null,
        float[]? zoneCentersY = null,
        bool[]? zoneEnabled = null,
        bool clipToTubes = true,
        int zoneShape = 0,
        bool rectFromHud = true,
        float rectWidth = 1200f,
        float rectHeight = 960f,
        float rectInset = 0f,
        float rectSoftness = 0.002f,
        bool heatKeyOnly = false,
        float heatKeyThreshold = 0.15f,
        float heatKeySoftness = 0.08f,
        float heatFloor = 0f,
        float heatGain = 2.5f,
        float heatPower = 0.6f,
        float traceThreshold = 0.30f,
        float traceSoftness = 0.15f,
        float traceIntensity = 1.2f,
        float outlineIntensity = 1.08f,
        float outlineWidth = 0.005f,
        bool fxNoise = false,
        bool fxFpsStuck = false,
        bool fxMotionBlur = false,
        bool fxGlitch = false,
        bool fxPixelate = false,
        string overlayTexture = "",
        float overlayOpacity = 1f,
        float vignetteStrength = 0f,
        float vignetteStart = 0.55f,
        float scintillation = 0f,
        int[]? extraModes = null,
        string hud = "")
    {
        var profile = new ThermalDeviceProfile(name, templateId);

        profile.Fov.Default = fov;
        profile.Resolution.Default = resolution;
        profile.RenderMode.Default = renderMode;
        profile.Zoom.Default = zoom;
        profile.CullLayers.Default = false;
        profile.ExcludeLayers.Default = "Player,PlayerRenderers,HighPolyCollider";
        profile.ZoneEnabled.Default = true;
        profile.ZoneCount.Default = zoneCount;
        profile.Opacity.Default = opacity;

        for (var z = 0; z < ThermalDeviceProfile.MaxZones; z++)
        {
            var slot = profile.Zones[z];

            slot.Enabled.Default = zoneEnabled != null && z < zoneEnabled.Length ? zoneEnabled[z] : z == 0;
            slot.CenterX.Default = zoneCentersX != null && z < zoneCentersX.Length ? zoneCentersX[z] : centerX;
            slot.CenterY.Default = zoneCentersY != null && z < zoneCentersY.Length ? zoneCentersY[z] : centerY;
            slot.Radius.Default = radius;
            slot.Softness.Default = softness;
        }

        profile.ClipToTubes.Default = clipToTubes;
        profile.ZoneShape.Default = zoneShape;
        profile.RectFromHud.Default = rectFromHud;
        profile.RectWidth.Default = rectWidth;
        profile.RectHeight.Default = rectHeight;
        profile.RectOffsetX.Default = 0f;
        profile.RectOffsetY.Default = 0f;
        profile.RectInset.Default = rectInset;
        profile.RectSoftness.Default = rectSoftness;
        profile.TubeMaskScale.Default = 1f;
        profile.TubeMaskSoftness.Default = 0.02f;
        profile.TubeMaskChannel.Default = 0;
        profile.HeatKeyOnly.Default = heatKeyOnly;
        profile.HeatKeyThreshold.Default = heatKeyThreshold;
        profile.HeatKeySoftness.Default = heatKeySoftness;
        profile.HotColor.Default = hot ?? new Color(1.0f, 0.357f, 0.0f, 1f);
        profile.ColdColor.Default = cold ?? new Color(0.0f, 0.02f, 0.0f, 1f);
        profile.HeatFloor.Default = heatFloor;
        profile.HeatGain.Default = heatGain;
        profile.HeatPower.Default = heatPower;
        profile.TraceThreshold.Default = traceThreshold;
        profile.TraceSoftness.Default = traceSoftness;
        profile.TraceIntensity.Default = traceIntensity;
        profile.OutlineIntensity.Default = outlineIntensity;
        profile.OutlineWidth.Default = outlineWidth;
        profile.FxNoise.Default = fxNoise;
        profile.FxFpsStuck.Default = fxFpsStuck;
        profile.FxMotionBlur.Default = fxMotionBlur;
        profile.FxGlitch.Default = fxGlitch;
        profile.FxPixelate.Default = fxPixelate;
        profile.UnsharpRadius.Default = 0f;
        profile.UnsharpBias.Default = 0f;
        profile.OverlayTexture.Default = overlayTexture;
        profile.OverlayOpacity.Default = overlayOpacity;
        profile.OverlayTint.Default = Color.white;
        profile.VignetteStrength.Default = vignetteStrength;
        profile.VignetteStart.Default = vignetteStart;
        profile.ScintillationStrength.Default = scintillation;
        profile.ScintillationScale.Default = 6f;
        profile.ScintillationSpeed.Default = 12f;

        profile.ExtraModes = extraModes ?? [];
        profile.HudPrefab = hud;

        ProfileCache.Add(profile);
    }
}
