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

       
        // To retune a device, switch TuningEnabled on and add tunable: true to its line. Anything
        // not listed uses Register's defaults.

        Register("PSQ-42", "6af9ccf0250e9b143053f7c8",
            zoom: 1f,
            zoneShape: 1, rectFromHud: false, rectWidth: 550.9389f, rectHeight: 558.6385f, rectOffsetX: 225.3521f, rectInset: 21.12676f, rectSoftness: 0.007042254f,
            opacity: 0.72f,
            clipToTubes: true, tubeMaskScale: 0.7887324f, tubeMaskSoftness: 0.08338028f,
            heatKeyOnly: true,
            heatPower: 0.3896714f, traceIntensity: 1.547418f, outlineIntensity: 2.817089f, outlineWidth: 0.002234742f,
            fxNoise: true, fxFpsStuck: true, fxMotionBlur: true, fxGlitch: true,
            overlayOpacity: 0.7042254f,
            modes: [ThermalMode.Outline, ThermalMode.WhiteHot, ThermalMode.BlackHot],
            hud: "FPanoHud", hudOffsetY: 15.21127f, hudScale: 0.6384977f,
            offInCycle: true);

        //Register("ECOTI-PVS-31", "6aa58d6c394c0807cb0af580", heatKeyOnly: true, zoneCount: 2, zoneCentersX: [0.37f, 0.63f], zoneCentersY: [0.5f, 0.5f], zoneEnabled: [true, true], radius: 0.26f, softness: 0.06f, modes: [ThermalMode.Outline, ThermalMode.WhiteHot], tunable: true);

        Register("F-PANO Fused", "6aece3759b55c16086112d9e",
            hot: new Color(1f, 1f, 0.6666667f, 1f),
            fov: 40f, zoom: 1f,
            zoneCount: 1,
            zoneCentersX: [0.5016901f, 0.41f, 0.59f, 0.78f],
            zoneCentersY: [0.48f, 0.5f, 0.5f, 0.48f],
            zoneEnabled: [true, false, false, false],
            zoneRadius: [0.4987794f, 0.2f, 0.2f, 0.2f],
            zoneSoftness: [0.2084977f, 0.07f, 0.07f, 0.07f],
            zoneShape: 0, rectFromHud: false, rectWidth: 532.3944f, rectHeight: 587.6526f, rectOffsetX: 225.3521f, rectInset: 21.12676f, rectSoftness: 0.009042257f,
            clipToTubes: true, tubeMaskScale: 0.7816901f, tubeMaskSoftness: 0.1030986f,
            heatKeyOnly: true,
            traceThreshold: 0.2530517f, traceSoftness: 0.3685446f, traceIntensity: 1.838498f, outlineWidth: 0.003840375f,
            fxNoise: true, fxFpsStuck: true, fxMotionBlur: true, fxGlitch: true,
            overlayOpacity: 0.6995305f, overlayTint: new Color(0f, 0.9529412f, 1f, 1f),
            modes: [ThermalMode.Outline, ThermalMode.WhiteHot, ThermalMode.BlackHot],
            hud: "FPanoHud", hudScale: 0.6795775f,
            offInCycle: true);

        // PVS-14 with a SkeetIR 
        Register("SkeetIR / PVS-14", "6af8bebd979735ba4efe9805",
            hot: new Color(1f, 1f, 1f, 1f), cold: new Color(0f, 0f, 0f, 1f),
            fov: 25.6338f, zoom: 1f,
            zoneCount: 1,
            zoneCentersX: [0.37f, 0.63f],
            zoneCentersY: [0.5f, 0.5f],
            zoneEnabled: [true, false],
            radius: 0.26f, softness: 0.10f,
            zoneShape: 1, rectFromHud: false, rectWidth: 606.5728f, rectHeight: 509.39f, rectOffsetX: -239.4366f, rectInset: 49.29578f, rectSoftness: 0.01690141f,
            clipToTubes: true, tubeMaskScale: 0.7394366f, tubeMaskSoftness: 0.2985916f,
            heatKeyOnly: false, heatKeyBackground: 1f,
            fxNoise: true, fxFpsStuck: true, fxMotionBlur: true, fxGlitch: true,
            modes: [ThermalMode.WhiteHot, ThermalMode.BlackHot, ThermalMode.EdgeDetect],
            hud: "SkeetIrHud", hudNeedsThermal: true, hudOffsetX: -225.3521f, hudOffsetY: 45.6338f);

        Plugin.logger!.LogInfo($"[WTT-CAG] Registered {ProfileCache.Count} thermal device profile(s).");
    }

    public static void RefreshActive()
    {
        var player = Singleton<GameWorld>.Instance?.MainPlayer;

        // With the player's overlay setting off, no device is ever active and the goggles stay stock.
        var overlaysEnabled = !(ThermalConfigSection.DisableThermalOverlays?.Value ?? false);
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
        float[]? zoneRadius = null,
        float[]? zoneSoftness = null,
        bool clipToTubes = true,
        float tubeMaskScale = 1f,
        float tubeMaskSoftness = 0.02f,
        int zoneShape = 0,
        bool rectFromHud = true,
        float rectWidth = 1200f,
        float rectHeight = 960f,
        float rectOffsetX = 0f,
        float rectOffsetY = 0f,
        float rectInset = 0f,
        float rectSoftness = 0.002f,
        bool heatKeyOnly = false,
        float heatKeyThreshold = 0.15f,
        float heatKeySoftness = 0.08f,
        float heatKeyBackground = 0f,
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
        Color? overlayTint = null,
        ThermalMode[]? modes = null,
        string hud = "",
        bool hudNeedsThermal = false,
        float hudOffsetX = 0f,
        float hudOffsetY = 0f,
        float hudScale = 1f,
        bool tunable = false,
        bool offInCycle = false)
    {
        var profile = new ThermalDeviceProfile(name, templateId);

        profile.Fov.Default = fov;
        profile.Resolution.Default = resolution;
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
            slot.Radius.Default = zoneRadius != null && z < zoneRadius.Length ? zoneRadius[z] : radius;
            slot.Softness.Default = zoneSoftness != null && z < zoneSoftness.Length ? zoneSoftness[z] : softness;
        }

        profile.ClipToTubes.Default = clipToTubes;
        profile.ZoneShape.Default = zoneShape;
        profile.RectFromHud.Default = rectFromHud;
        profile.RectWidth.Default = rectWidth;
        profile.RectHeight.Default = rectHeight;
        profile.RectOffsetX.Default = rectOffsetX;
        profile.RectOffsetY.Default = rectOffsetY;
        profile.RectInset.Default = rectInset;
        profile.RectSoftness.Default = rectSoftness;
        profile.TubeMaskScale.Default = tubeMaskScale;
        profile.TubeMaskSoftness.Default = tubeMaskSoftness;
        profile.TubeMaskChannel.Default = 0;
        profile.HeatKeyOnly.Default = heatKeyOnly;
        profile.HeatKeyThreshold.Default = heatKeyThreshold;
        profile.HeatKeySoftness.Default = heatKeySoftness;
        profile.HeatKeyBackground.Default = heatKeyBackground;
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
        profile.OverlayTint.Default = overlayTint ?? Color.white;

        profile.Modes = modes is { Length: > 0 } ? modes : [ThermalMode.Outline];
        profile.HudPrefab = hud;
        profile.Tunable = tunable;
        profile.OffInCycle = offInCycle;
        profile.HudNeedsThermal = hudNeedsThermal;
        profile.HudOffsetX.Default = hudOffsetX;
        profile.HudOffsetY.Default = hudOffsetY;
        profile.HudScale.Default = hudScale;

        ProfileCache.Add(profile);
    }
}