using BSG.CameraEffects;
using UnityEngine;
using WTT_CAG.Client.Config;
using WTT_CAG.Client.Content.Thermals.Fusion;
using WTT_CAG.Client.Core.Thermal;
using WTT_CAG.Client.Core;

namespace WTT_CAG.Client.Content.Thermals;

// Every per-frame shader property for the optic, zone and composite materials.
public static class ThermalMaterialProperties
{
    private const int ClearRenderMode = 3;

    private static readonly int UseRampColor = Shader.PropertyToID("_UseRampColor");
    private static readonly int HotColor = Shader.PropertyToID("_HotColor");
    private static readonly int ColdColor = Shader.PropertyToID("_ColdColor");
    private static readonly int HeatGain = Shader.PropertyToID("_HeatGain");
    private static readonly int HeatPower = Shader.PropertyToID("_HeatPower");
    private static readonly int HeatFloor = Shader.PropertyToID("_HeatFloor");
    private static readonly int DebugHeat = Shader.PropertyToID("_DebugHeat");
    private static readonly int TraceThreshold = Shader.PropertyToID("_TraceThreshold");
    private static readonly int TraceSoftness = Shader.PropertyToID("_TraceSoftness");
    private static readonly int TraceIntensity = Shader.PropertyToID("_TraceIntensity");
    private static readonly int OutlineIntensity = Shader.PropertyToID("_OutlineIntensity");
    private static readonly int OutlineWidth = Shader.PropertyToID("_OutlineWidth");
    private static readonly int GlowWidth = Shader.PropertyToID("_GlowWidth");
    private static readonly int GlowIntensity = Shader.PropertyToID("_GlowIntensity");
    private static readonly int BaseOpacity = Shader.PropertyToID("_BaseOpacity");
    private static readonly int RenderMode = Shader.PropertyToID("_RenderMode");

    private static readonly int ThermalZoneRT = Shader.PropertyToID("_ThermalZoneRT");
    private static readonly int ThermalZoneEnabled = Shader.PropertyToID("_ThermalZoneEnabled");
    private static readonly int ThermalZoneOpacity = Shader.PropertyToID("_ThermalZoneOpacity");
    private static readonly int ThermalZoneZoom = Shader.PropertyToID("_ThermalZoneZoom");
    private static readonly int ThermalZoneUvScale = Shader.PropertyToID("_ThermalZoneUvScale");
    private static readonly int Zones = Shader.PropertyToID("_Zones");
    private static readonly int ZoneCount = Shader.PropertyToID("_ZoneCount");
    private static readonly int ZoneShape = Shader.PropertyToID("_ZoneShape");
    private static readonly int ZoneRect = Shader.PropertyToID("_ZoneRect");
    private static readonly int ZoneRectSoftness = Shader.PropertyToID("_ZoneRectSoftness");
    private static readonly int ZoneAspect = Shader.PropertyToID("_ZoneAspect");
    private static readonly int ZoneDebugFill = Shader.PropertyToID("_ZoneDebugFill");
    private static readonly int HeatKeyOnly = Shader.PropertyToID("_HeatKeyOnly");
    private static readonly int HeatKeyThreshold = Shader.PropertyToID("_HeatKeyThreshold");
    private static readonly int HeatKeySoftness = Shader.PropertyToID("_HeatKeySoftness");
    private static readonly int HeatKeyBackground = Shader.PropertyToID("_HeatKeyBackground");
    private static readonly int TubeClip = Shader.PropertyToID("_TubeClip");
    private static readonly int TubeMask = Shader.PropertyToID("_TubeMask");
    private static readonly int TubeInvMaskSize = Shader.PropertyToID("_TubeInvMaskSize");
    private static readonly int TubeInvAspect = Shader.PropertyToID("_TubeInvAspect");
    private static readonly int TubeCameraAspect = Shader.PropertyToID("_TubeCameraAspect");
    private static readonly int TubeMaskSoftness = Shader.PropertyToID("_TubeMaskSoftness");
    private static readonly int TubeMaskChannel = Shader.PropertyToID("_TubeMaskChannel");
    private static readonly int TubeMaskInvert = Shader.PropertyToID("_TubeMaskInvert");
    private static readonly int TubeMaskSoft = Shader.PropertyToID("_TubeMaskSoft");

    // The screen shape the circle positions were tuned at (3440 x 1440).
    private const float TunedAspect = 3440f / 1440f;
    private static readonly int OverlayTex = Shader.PropertyToID("_OverlayTex");
    private static readonly int OverlayOpacity = Shader.PropertyToID("_OverlayOpacity");
    private static readonly int OverlayTint = Shader.PropertyToID("_OverlayTint");
    private static readonly int ScreenUvScale = Shader.PropertyToID("_ScreenUvScale");
    private static readonly int ScreenUvOffset = Shader.PropertyToID("_ScreenUvOffset");

    private static readonly Vector4[] ZoneBuffer = new Vector4[ThermalDeviceProfile.MaxZones];

    private static string? _lastTubeClipState;
    private static string? _lastScaleReport;

    public static void ApplyOptic(Material material, int scopeMode)
    {
        material.SetFloat(UseRampColor, 0f);
        material.SetColor(HotColor, ThermalConfigSection.HotColor.Value);
        material.SetColor(ColdColor, ThermalConfigSection.ColdColor.Value);
        material.SetFloat(HeatGain, ThermalConfigSection.HeatGain.Value);
        material.SetFloat(HeatPower, ThermalConfigSection.HeatContrast.Value);
        material.SetFloat(HeatFloor, ThermalConfigSection.HeatFloor.Value);
        material.SetFloat(DebugHeat, ThermalConfigSection.DebugShowRawHeat.Value ? 1f : 0f);
        material.SetFloat(TraceThreshold, ThermalConfigSection.TraceThreshold.Value);
        material.SetFloat(TraceSoftness, ThermalConfigSection.TraceSoftness.Value);
        material.SetFloat(TraceIntensity, ThermalConfigSection.TraceBrightness.Value);
        material.SetFloat(OutlineIntensity, ThermalConfigSection.OutlineIntensity.Value);
        material.SetFloat(OutlineWidth, ThermalConfigSection.OutlineWidth.Value);
        material.SetFloat(GlowWidth, ThermalConfigSection.GlowWidth.Value);
        material.SetFloat(GlowIntensity, ThermalConfigSection.GlowIntensity.Value);
        material.SetFloat(BaseOpacity, 0f);
        material.SetFloat(RenderMode, scopeMode is >= 0 and <= ClearRenderMode ? scopeMode : ClearRenderMode);
    }

    public static void ApplyZone(Material material, ThermalDeviceProfile? profile)
    {
        if (profile == null) { return; }

        var mode = FusionThermalState.CurrentMode(profile);

        material.SetFloat(UseRampColor, 0f);
        material.SetColor(HotColor, mode.HotColor ?? profile.HotColor.Value);
        material.SetColor(ColdColor, mode.ColdColor ?? profile.ColdColor.Value);
        material.SetFloat(HeatGain, profile.HeatGain.Value);
        material.SetFloat(HeatPower, profile.HeatPower.Value);
        material.SetFloat(HeatFloor, profile.HeatFloor.Value);
        material.SetFloat(DebugHeat, ThermalConfigSection.DebugShowRawHeat.Value ? 1f : 0f);
        material.SetFloat(TraceThreshold, profile.TraceThreshold.Value);
        material.SetFloat(TraceSoftness, profile.TraceSoftness.Value);
        material.SetFloat(TraceIntensity, profile.TraceIntensity.Value);
        material.SetFloat(OutlineIntensity, profile.OutlineIntensity.Value);
        material.SetFloat(OutlineWidth, profile.OutlineWidth.Value);
        material.SetFloat(RenderMode, mode.RenderMode);
    }

    public static void ApplyComposite(Material material, ThermalDeviceProfile? profile, NightVision nightVision)
    {
        if (profile == null)
        {
            material.SetFloat(ThermalZoneEnabled, 0f);
            return;
        }

        var zoneTexture = FusionThermalZoneCamera.Instance ? FusionThermalZoneCamera.Instance!.ThermalTexture : null;

        if (zoneTexture)
        {
            material.SetTexture(ThermalZoneRT, zoneTexture);
        }

        material.SetFloat(ThermalZoneEnabled, profile.ZoneEnabled.Value && FusionThermalState.ThermalOn && nightVision.On ? 1f : 0f);
        material.SetFloat(ThermalZoneOpacity, profile.Opacity.Value);

        var screenAspect = (float)Screen.width / Mathf.Max(Screen.height, 1);
        var count = Mathf.Clamp(profile.ZoneCount.Value, 1, ThermalDeviceProfile.MaxZones);

        for (var z = 0; z < ThermalDeviceProfile.MaxZones; z++)
        {
            var slot = profile.Zones[z];
            var live = z < count && slot.Enabled.Value;

            // Circle positions were tuned at 21:9. Scaling their offset from centre keeps them at the
            // same place relative to the HUD, which is laid out in screen-height units, at any aspect.
            var centreX = 0.5f + (slot.CenterX.Value - 0.5f) * (TunedAspect / Mathf.Max(screenAspect, 0.01f));

            ZoneBuffer[z] = live ? new Vector4(centreX, slot.CenterY.Value, slot.Radius.Value, slot.Softness.Value) : Vector4.zero;
        }

        material.SetVectorArray(Zones, ZoneBuffer);
        material.SetFloat(ZoneCount, count);

        var camera = nightVision.GetComponent<Camera>();
        var aspect = camera ? camera.aspect : 1.777f;

        var rectangle = profile.ZoneShape.Value == 1;

        material.SetFloat(ZoneShape, rectangle ? 1f : 0f);

        if (rectangle)
        {
            material.SetVector(ZoneRect, ZoneRectangle(profile, aspect));
            material.SetFloat(ZoneRectSoftness, profile.RectSoftness.Value);
        }

        ApplyTubeMask(material, profile, nightVision, aspect);

        var heatKeyOnly = FusionThermalState.CurrentMode(profile).HeatKeyOnly ?? profile.HeatKeyOnly.Value;

        material.SetFloat(HeatKeyOnly, heatKeyOnly ? 1f : 0f);
        material.SetFloat(HeatKeyThreshold, profile.HeatKeyThreshold.Value);
        material.SetFloat(HeatKeySoftness, profile.HeatKeySoftness.Value);
        material.SetFloat(HeatKeyBackground, profile.HeatKeyBackground.Value);

        material.SetFloat(ZoneAspect, aspect);
        material.SetFloat(ThermalZoneZoom, profile.Zoom.Value * FusionThermalState.ZoomFactor(profile));
        material.SetFloat(ThermalZoneUvScale, FusionThermalZoneCamera.ZoneUVScale);

        if (BuildFlags.DebugBuild)
        {
            var report = $"{profile.Name}: {FusionThermalZoneCamera.ScaleReport}, zoom {profile.Zoom.Value:F2}, path {(FusionThermalController.PostUpscaleActive ? "overlay camera" : "FPS camera")}";

            if (report != _lastScaleReport)
            {
                _lastScaleReport = report;
                CagLog.Info($"[WTT-CAG] {report}");
            }
        }
        material.SetFloat(ZoneDebugFill, ThermalConfigSection.ZoneDebugFill.Value ? 1f : 0f);

        var hud = FusionThermalHud.Instance;
        var overlay = hud && hud!.Profile == profile && hud.IsVisible && hud.Texture ? hud.Texture : profile.ResolveOverlayTexture();

        if (overlay)
        {
            material.SetTexture(OverlayTex, overlay);
            material.SetFloat(OverlayOpacity, profile.OverlayOpacity.Value);
        }
        else
        {
            material.SetFloat(OverlayOpacity, 0f);
        }

        material.SetColor(OverlayTint, profile.OverlayTint.Value);

        material.SetVector(ScreenUvScale, new Vector4(1f, 1f, 0f, 0f));
        material.SetVector(ScreenUvOffset, Vector4.zero);
    }

    private static void LogTubeClip(ThermalDeviceProfile profile, string state)
    {
        var line = $"{profile.Name}: tube clip {state}";

        if (line == _lastTubeClipState) { return; }

        _lastTubeClipState = line;
        CagLog.Info($"[WTT-CAG] {line}");
    }

    private static Vector4 ZoneRectangle(ThermalDeviceProfile profile, float aspect)
    {
        var unitY = 1f / FusionThermalHud.CanvasHeight;
        var unitX = unitY / Mathf.Max(aspect, 0.01f);

        var hud = FusionThermalHud.Instance;
        Vector4 rect;

        if (!profile.RectFromHud.Value || !hud || hud!.Profile != profile || !hud.TryGetDisplayRect(out rect))
        {
            var centreX = 0.5f + profile.RectOffsetX.Value * unitX;
            var centreY = 0.5f + profile.RectOffsetY.Value * unitY;
            var halfWidth = profile.RectWidth.Value * 0.5f * unitX;
            var halfHeight = profile.RectHeight.Value * 0.5f * unitY;

            rect = new Vector4(centreX - halfWidth, centreY - halfHeight, centreX + halfWidth, centreY + halfHeight);
        }

        var inset = profile.RectInset.Value;

        return new Vector4(rect.x + inset * unitX, rect.y + inset * unitY, rect.z - inset * unitX, rect.w - inset * unitY);
    }

    private static void ApplyTubeMask(Material material, ThermalDeviceProfile profile, NightVision nightVision, float aspect)
    {
        if (!profile.ClipToTubes.Value)
        {
            material.SetFloat(TubeClip, 0f);
            LogTubeClip(profile, "off");
            return;
        }

        // Default: the mask in the shared TextureMask - in practice the game's thermal mask, which every
        // goggle's Tube Mask Scale and Edge were tuned against. UseGoggleTubeMask uses the goggle's real
        // tube mask (NightVision.Mask) instead, which needs that goggle retuned.
        var useGoggleMask = profile.UseGoggleTubeMask.Value;
        var textureMask = nightVision.TextureMask;

        var mask = useGoggleMask ? nightVision.Mask : textureMask != null ? textureMask.Mask : null;
        var maskSize = useGoggleMask ? nightVision.MaskSize : textureMask != null ? textureMask.Size : 1f;

        if (!mask)
        {
            material.SetFloat(TubeClip, 0f);
            LogTubeClip(profile, $"requested but not applied - no {(useGoggleMask ? "goggle tube" : "TextureMask")} mask on this NightVision");
            return;
        }

        var size = maskSize * profile.TubeMaskScale.Value;

        if (size < 0.0001f) { size = 1f; }

        LogTubeClip(profile, $"on - {(useGoggleMask ? "goggle tube" : "thermal")} mask '{mask!.name}' {mask.width}x{mask.height}, size {maskSize} x {profile.TubeMaskScale.Value}, channel {profile.TubeMaskChannel.Value}");

        material.SetFloat(TubeClip, 1f);
        material.SetTexture(TubeMask, mask);
        material.SetFloat(TubeInvMaskSize, 1f / size);
        material.SetFloat(TubeInvAspect, (float)mask!.height / mask.width);
        material.SetFloat(TubeCameraAspect, aspect);
        material.SetFloat(TubeMaskSoftness, profile.TubeMaskSoftness.Value);
        material.SetFloat(TubeMaskChannel, profile.TubeMaskChannel.Value);

        // BSG's masks mark the surround rather than the tube.
        material.SetFloat(TubeMaskInvert, 1f);
        material.SetFloat(TubeMaskSoft, profile.SoftTubeMask.Value ? 1f : 0f);
    }
}
