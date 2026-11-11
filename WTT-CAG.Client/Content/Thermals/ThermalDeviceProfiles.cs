using System.Collections.Generic;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using UnityEngine;
using WTT_CAG.Client.Core.Thermal;

namespace WTT_CAG.Client.Content.Thermals;

public static class ThermalDeviceProfiles
{
    private static readonly List<ThermalDeviceProfile> ProfileCache = [];
    public static ThermalDeviceProfile? Active;

    public static void Init(ConfigFile cfg)
    {
        ProfileCache.Clear();
        Active = null;

        Register(cfg, "BNVG-Fused", "6a2dcf2e11cf8134015c0563", hot: new Color(1.0f, 0.357f, 0.0f, 1f), renderMode: 2, zoom: 1.5f, radius: 0.35f, opacity: 0.72f, heatFloor: 0f, traceThreshold: 0.30f, outlineIntensity: 1.08f);
        Register(cfg, "ECOTI-PVS-31", "6aa58d6c394c0807cb0af580", renderMode: 2, heatKeyOnly: true, zoneCount: 2, zoneCentersX: [0.37f, 0.63f], zoneCentersY: [0.5f, 0.5f], zoneEnabled: [true, true], radius: 0.26f, softness: 0.06f);
        Register(cfg, "F-PANO Fused", "6aa58e21003ae80c900af582", renderMode: 2, heatKeyOnly: true, zoneCount: 4, zoneCentersX: [0.22f, 0.41f, 0.59f, 0.78f], zoneCentersY: [0.48f, 0.50f, 0.50f, 0.48f], zoneEnabled: [true, true, true, true], radius: 0.20f, softness: 0.07f);
        Register(cfg, "Breach / PVS-14", "6aa58e595da66037460af585", renderMode: 1, heatKeyOnly: false, hot: new Color(1f, 1f, 1f, 1f), cold: new Color(0f, 0f, 0f, 1f), zoneCount: 2, zoneCentersX: [0.37f, 0.63f], zoneCentersY: [0.5f, 0.5f], zoneEnabled: [true, false], radius: 0.26f, softness: 0.10f, opacity: 0.95f);

        Plugin.logger!.LogInfo($"[WTT-CAG] Registered {ProfileCache.Count} thermal device profile(s).");
    }

    public static void RefreshActive()
    {
        var player = Singleton<GameWorld>.Instance?.MainPlayer;
        var templateId = player?.NightVisionObserver?.Component?.Item?.StringTemplateId;

        Active = ProfileCache.Find(profile => profile.TemplateId == templateId);
    }

    private static void Register(
        ConfigFile cfg,
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
        float scintillation = 0f)
    {
        var profile = new ThermalDeviceProfile(name, templateId);

        var cam = $"{name} — Zone Camera";
        var place = $"{name} — Zone Placement";
        var image = $"{name} — Zone Image";

        profile.Fov = cfg.Bind(cam, "FOV", fov, new ConfigDescription("Field of view of the thermal camera. Narrower than the NVG view means the circle is magnified, as on the real device.", new AcceptableValueRange<float>(5f, 90f)));
        profile.Resolution = cfg.Bind(cam, "Resolution", resolution, new ConfigDescription("Horizontal resolution of the thermal render. Lower is cheaper; 320 approximates a real device's sensor.", new AcceptableValueRange<int>(128, 1024)));
        profile.RenderMode = cfg.Bind(cam, "Render Mode", renderMode, new ConfigDescription("0 = Trace, 1 = Full, 2 = Outline.", new AcceptableValueRange<int>(0, 2)));
        profile.Zoom = cfg.Bind(cam, "Zoom", zoom, new ConfigDescription("1 keeps the thermal aligned 1:1 with the world behind the circle. Above 1 magnifies.", new AcceptableValueRange<float>(0.25f, 4f)));
        profile.CullLayers = cfg.Bind(cam, "Cull Layers", false, "Off by default: a real thermal sees whatever is in front of it, including your own hands.");
        profile.ExcludeLayers = cfg.Bind(cam, "Exclude Layers", "Player,PlayerRenderers,HighPolyCollider", "Comma-separated layer names to hide when Cull Layers is on. Unknown names are ignored and listed in the log.");

        profile.ZoneEnabled = cfg.Bind(place, "Enabled", true, "Master switch for this goggle's thermal channel.");
        profile.ZoneCount = cfg.Bind(place, "Zone Count", zoneCount, new ConfigDescription("How many thermal circles to draw. 1 for a single clip-on module, 2 for a dual-tube set, 4 for a panoramic.", new AcceptableValueRange<int>(1, ThermalDeviceProfile.MaxZones)));
        profile.Opacity = cfg.Bind(place, "Opacity", opacity, new ConfigDescription("How strongly the thermal replaces the night vision image, across all zones.", new AcceptableValueRange<float>(0f, 1f)));

        for (var z = 0; z < ThermalDeviceProfile.MaxZones; z++)
        {
            var slot = new ThermalZoneSlot();
            var zs = $"{name} — Zone {z + 1}";

            var onByDefault = zoneEnabled != null && z < zoneEnabled.Length ? zoneEnabled[z] : z == 0;

            var cx = zoneCentersX != null && z < zoneCentersX.Length ? zoneCentersX[z] : centerX;
            var cy = zoneCentersY != null && z < zoneCentersY.Length ? zoneCentersY[z] : centerY;

            slot.Enabled = cfg.Bind(zs, "Enabled", onByDefault, "Draw thermal in this zone. A dual-tube goggle with thermal in one eye only leaves the other zone off.");
            slot.CenterX = cfg.Bind(zs, "Center X", cx, new ConfigDescription("Horizontal centre, in screen fractions.", new AcceptableValueRange<float>(0f, 1f)));
            slot.CenterY = cfg.Bind(zs, "Center Y", cy, new ConfigDescription("Vertical centre, in screen fractions.", new AcceptableValueRange<float>(0f, 1f)));
            slot.Radius = cfg.Bind(zs, "Radius", radius, new ConfigDescription("Radius of this circle.", new AcceptableValueRange<float>(0.02f, 1.5f)));
            slot.Softness = cfg.Bind(zs, "Edge", softness, new ConfigDescription("Edge softness. Raise on adjacent tubes so their thermal blends where they meet rather than butting hard.", new AcceptableValueRange<float>(0f, 0.5f)));

            profile.Zones[z] = slot;
        }

        profile.ClipToTubes = cfg.Bind(place, "Clip To Tubes", clipToTubes, "Clip thermal to the goggle's own tube mask, so nothing spills into the black outside the tubes. Uses the same mask texture BSG cuts the night vision with, so it aligns by construction.");
        profile.TubeMaskScale = cfg.Bind(place, "Tube Mask Scale", 1f, new ConfigDescription("Fine adjustment if the clip does not line up with the tubes. 1 uses the game's own mask size unmodified.", new AcceptableValueRange<float>(0.5f, 2f)));
        profile.TubeMaskSoftness = cfg.Bind(place, "Tube Mask Edge", 0.02f, new ConfigDescription("Softens the clip edge so thermal fades into the tube rim.", new AcceptableValueRange<float>(0f, 0.3f)));
        profile.TubeMaskChannel = cfg.Bind(place, "Tube Mask Channel", 0, new ConfigDescription("Which channel of the mask texture is the tube shape. 0 = alpha, 1 = red. Try 1 if clipping removes everything.", new AcceptableValueRange<int>(0, 1)));
        profile.TubeMaskInvert = cfg.Bind(place, "Tube Mask Invert", true, "Flip the mask's sense. BSG's tube masks are authored to DARKEN the area OUTSIDE the tubes, so the bright part of the texture is the surround rather than the tube — taken raw, the clip keeps exactly the wrong region. On by default for that reason; turn off if a goggle's mask is authored the other way round.");

        profile.HeatKeyOnly = cfg.Bind(place, "Heat Only", heatKeyOnly, "ON: draw only hot pixels and let the night vision show through the cold ones, so the circle is an overlay rather than a cutout. OFF: the circle replaces the NVG image, cold areas included, which reads as black.");
        profile.HeatKeyThreshold = cfg.Bind(place, "Heat Only Threshold", heatKeyThreshold, new ConfigDescription("How bright a thermal pixel must be to be drawn when Heat Only is on. Raise to show only the hottest objects.", new AcceptableValueRange<float>(0f, 1f)));
        profile.HeatKeySoftness = cfg.Bind(place, "Heat Only Softness", heatKeySoftness, new ConfigDescription("Edge softness of the Heat Only cutoff. Higher fades hot areas into the night vision instead of cutting sharply.", new AcceptableValueRange<float>(0.001f, 0.5f)));

        profile.HotColor = cfg.Bind(image, "Hot Color", hot ?? new Color(1.0f, 0.357f, 0.0f, 1f), "Colour of hot objects.");
        profile.ColdColor = cfg.Bind(image, "Cold Color", cold ?? new Color(0.0f, 0.02f, 0.0f, 1f), "Colour of cold areas. Near-black unless Heat Only is on, in which case cold is transparent and this barely matters.");
        profile.HeatFloor = cfg.Bind(image, "Heat Floor", heatFloor, new ConfigDescription("Cold cutoff. Raise to reject cool objects.", new AcceptableValueRange<float>(0f, 1f)));
        profile.HeatGain = cfg.Bind(image, "Heat Gain", heatGain, new ConfigDescription("Overall heat strength multiplier.", new AcceptableValueRange<float>(1f, 4f)));
        profile.HeatPower = cfg.Bind(image, "Heat Power", heatPower, new ConfigDescription("Contrast curve. Below 1 brightens mid heat.", new AcceptableValueRange<float>(0.2f, 3f)));
        profile.TraceThreshold = cfg.Bind(image, "Trace Threshold", traceThreshold, new ConfigDescription("How hot something must be to show in Trace/Outline modes.", new AcceptableValueRange<float>(0f, 1f)));
        profile.TraceSoftness = cfg.Bind(image, "Trace Softness", traceSoftness, new ConfigDescription("Threshold band softness.", new AcceptableValueRange<float>(0.01f, 0.5f)));
        profile.TraceIntensity = cfg.Bind(image, "Trace Brightness", traceIntensity, new ConfigDescription("Brightness of the thermal colour.", new AcceptableValueRange<float>(0f, 2f)));
        profile.OutlineIntensity = cfg.Bind(image, "Outline Intensity", outlineIntensity, new ConfigDescription("Sobel edge strength in Outline mode.", new AcceptableValueRange<float>(0f, 5f)));
        profile.OutlineWidth = cfg.Bind(image, "Outline Width", outlineWidth, new ConfigDescription("Edge sampling thickness.", new AcceptableValueRange<float>(0.001f, 0.02f)));

        var fx = $"{name} — Sensor";

        profile.FxNoise = cfg.Bind(fx, "Noise", fxNoise, "Sensor noise, using the game's own thermal noise rather than a reimplementation. Uncooled microbolometers are noisier than cooled ones, so a cheap clip-on wants this and a dedicated sight may not.");
        profile.FxFpsStuck = cfg.Bind(fx, "Low Refresh Rate", fxFpsStuck, "Emulate a slow sensor refresh, so the thermal image updates in steps rather than every frame. This is the single most recognisable thermal artefact — export-restricted civilian units in particular run far below display framerate.");
        profile.FxMotionBlur = cfg.Bind(fx, "Motion Blur", fxMotionBlur, "Smearing as the sensor integrates across a frame. Pairs with a low refresh rate; on its own it tends to look like a post effect rather than a sensor.");
        profile.FxGlitch = cfg.Bind(fx, "Glitch", fxGlitch, "Occasional frame corruption. Sparing use only — constant glitching reads as broken hardware rather than a working device.");
        profile.FxPixelate = cfg.Bind(fx, "Pixelate", fxPixelate, "Coarsen the image to suggest a low sensor resolution. Consider lowering Zone Camera Resolution instead, which is the same idea applied at the source and costs less.");
        profile.UnsharpRadius = cfg.Bind(fx, "Sharpen Radius", 0f, new ConfigDescription("Edge sharpening radius. Thermal imagers apply their own edge enhancement; 0 disables.", new AcceptableValueRange<float>(0f, 4f)));
        profile.UnsharpBias = cfg.Bind(fx, "Sharpen Bias", 0f, new ConfigDescription("Strength of the edge sharpening.", new AcceptableValueRange<float>(0f, 4f)));

        var dev = $"{name} — Device Overlay";

        profile.OverlayTexture = cfg.Bind(dev, "Overlay Texture", overlayTexture, "Asset name of an RGBA texture in the bundle to draw over the whole view — bezel, reticle, tube artwork, dirt. Leave empty for none. Names that fail to load are reported once in the log.");
        profile.OverlayOpacity = cfg.Bind(dev, "Overlay Opacity", overlayOpacity, new ConfigDescription("How strongly the overlay texture is drawn.", new AcceptableValueRange<float>(0f, 1f)));
        profile.OverlayTint = cfg.Bind(dev, "Overlay Tint", Color.white, "Multiplied into the overlay texture. Lets one shared faceplate be recoloured per goggle instead of authoring several.");

        profile.VignetteStrength = cfg.Bind(dev, "Vignette Strength", vignetteStrength, new ConfigDescription("Darkening toward the edge of the view, as a tube falls off. 0 disables it.", new AcceptableValueRange<float>(0f, 1f)));
        profile.VignetteStart = cfg.Bind(dev, "Vignette Start", vignetteStart, new ConfigDescription("Distance from centre where the vignette begins.", new AcceptableValueRange<float>(0f, 1f)));
        profile.ScintillationStrength = cfg.Bind(dev, "Scintillation", scintillation, new ConfigDescription("Live sparkle from photon noise in an image intensifier. Subtle values read best; 0 disables it.", new AcceptableValueRange<float>(0f, 1f)));
        profile.ScintillationScale = cfg.Bind(dev, "Scintillation Grain", 6f, new ConfigDescription("Speck size. Higher is coarser.", new AcceptableValueRange<float>(1f, 32f)));
        profile.ScintillationSpeed = cfg.Bind(dev, "Scintillation Speed", 12f, new ConfigDescription("How fast the sparkle reshuffles.", new AcceptableValueRange<float>(0f, 60f)));

        ProfileCache.Add(profile);
    }
}
