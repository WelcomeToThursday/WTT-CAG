using BSG.CameraEffects;
using UnityEngine;
using WTT_CAG.Client.Config;
using WTT_CAG.Client.Content.Thermals.Fusion;
using WTT_CAG.Client.Core.Thermal;

namespace WTT_CAG.Client.Content.Thermals;

// Every per-frame shader property the thermal materials need, in one place.
// Values are read live from config so F12 changes show up immediately.
public static class ThermalMaterialProperties
{
    // Scope mode 3 on the Holosun is the plain red dot. Anything out of range falls back to it.
    private const int ClearRenderMode = 3;

    // ThermalTraceOverlay
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

    // FusionNVG
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
    private static readonly int TubeClip = Shader.PropertyToID("_TubeClip");
    private static readonly int TubeMask = Shader.PropertyToID("_TubeMask");
    private static readonly int TubeInvMaskSize = Shader.PropertyToID("_TubeInvMaskSize");
    private static readonly int TubeInvAspect = Shader.PropertyToID("_TubeInvAspect");
    private static readonly int TubeCameraAspect = Shader.PropertyToID("_TubeCameraAspect");
    private static readonly int TubeMaskSoftness = Shader.PropertyToID("_TubeMaskSoftness");
    private static readonly int TubeMaskChannel = Shader.PropertyToID("_TubeMaskChannel");
    private static readonly int TubeMaskInvert = Shader.PropertyToID("_TubeMaskInvert");
    private static readonly int OverlayTex = Shader.PropertyToID("_OverlayTex");
    private static readonly int OverlayOpacity = Shader.PropertyToID("_OverlayOpacity");
    private static readonly int OverlayTint = Shader.PropertyToID("_OverlayTint");
    private static readonly int VignetteStrength = Shader.PropertyToID("_VignetteStrength");
    private static readonly int VignetteStart = Shader.PropertyToID("_VignetteStart");
    private static readonly int Scintillation = Shader.PropertyToID("_Scintillation");
    private static readonly int ScintGrain = Shader.PropertyToID("_ScintGrain");
    private static readonly int ScintSpeed = Shader.PropertyToID("_ScintSpeed");
    private static readonly int ScreenUvScale = Shader.PropertyToID("_ScreenUvScale");
    private static readonly int ScreenUvOffset = Shader.PropertyToID("_ScreenUvOffset");

    // Reused every frame so SetVectorArray never allocates.
    private static readonly Vector4[] ZoneBuffer = new Vector4[ThermalDeviceProfile.MaxZones];

    private static bool _loggedMissingTubeMask;

    // The Holosun optic's ThermalTraceOverlay material. Tuning comes from the global thermal config.
    // Scope mode maps straight onto _RenderMode: 0 = Trace, 1 = Full, 2 = Outline, 3 = Clear.
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

    // The zone camera's ThermalTraceOverlay material. Tuning comes from the worn goggle's profile.
    public static void ApplyZone(Material material, ThermalDeviceProfile? profile)
    {
        if (profile == null) { return; }

        material.SetFloat(UseRampColor, 0f);
        material.SetColor(HotColor, profile.HotColor.Value);
        material.SetColor(ColdColor, profile.ColdColor.Value);
        material.SetFloat(HeatGain, profile.HeatGain.Value);
        material.SetFloat(HeatPower, profile.HeatPower.Value);
        material.SetFloat(HeatFloor, profile.HeatFloor.Value);
        material.SetFloat(DebugHeat, ThermalConfigSection.DebugShowRawHeat.Value ? 1f : 0f);
        material.SetFloat(TraceThreshold, profile.TraceThreshold.Value);
        material.SetFloat(TraceSoftness, profile.TraceSoftness.Value);
        material.SetFloat(TraceIntensity, profile.TraceIntensity.Value);
        material.SetFloat(OutlineIntensity, profile.OutlineIntensity.Value);
        material.SetFloat(OutlineWidth, profile.OutlineWidth.Value);
        material.SetFloat(RenderMode, FusionThermalState.RenderMode(profile));
    }

    // The FusionNVG material that draws the thermal zones over the night vision image.
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

        // Nothing is drawn over a tube that is switched off.
        material.SetFloat(ThermalZoneEnabled, profile.ZoneEnabled.Value && FusionThermalState.ThermalOn && nightVision.On ? 1f : 0f);
        material.SetFloat(ThermalZoneOpacity, profile.Opacity.Value);

        // Packed as xy = centre, z = radius, w = softness. A disabled zone is all zero, which the shader skips.
        var count = Mathf.Clamp(profile.ZoneCount.Value, 1, ThermalDeviceProfile.MaxZones);

        for (var z = 0; z < ThermalDeviceProfile.MaxZones; z++)
        {
            var slot = profile.Zones[z];
            var live = z < count && slot.Enabled.Value;

            ZoneBuffer[z] = live ? new Vector4(slot.CenterX.Value, slot.CenterY.Value, slot.Radius.Value, slot.Softness.Value) : Vector4.zero;
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

        material.SetFloat(HeatKeyOnly, profile.HeatKeyOnly.Value ? 1f : 0f);
        material.SetFloat(HeatKeyThreshold, profile.HeatKeyThreshold.Value);
        material.SetFloat(HeatKeySoftness, profile.HeatKeySoftness.Value);

        material.SetFloat(ZoneAspect, aspect);
        material.SetFloat(ThermalZoneZoom, profile.Zoom.Value);
        material.SetFloat(ThermalZoneUvScale, FusionThermalZoneCamera.ZoneUVScale);
        material.SetFloat(ZoneDebugFill, ThermalConfigSection.ZoneDebugFill.Value ? 1f : 0f);

        // A live HUD takes the overlay slot when the goggle has one; otherwise the static texture does.
        var hud = FusionThermalHud.Instance;
        var overlay = hud && hud!.Profile == profile && hud.Texture ? hud.Texture : profile.ResolveOverlayTexture();

        if (overlay)
        {
            material.SetTexture(OverlayTex, overlay);
            material.SetFloat(OverlayOpacity, profile.OverlayOpacity.Value);
        }
        else
        {
            // Zero opacity so the shader skips the sample rather than reading whatever was last bound.
            material.SetFloat(OverlayOpacity, 0f);
        }

        material.SetColor(OverlayTint, profile.OverlayTint.Value);
        material.SetFloat(VignetteStrength, profile.VignetteStrength.Value);
        material.SetFloat(VignetteStart, profile.VignetteStart.Value);
        material.SetFloat(Scintillation, profile.ScintillationStrength.Value);
        material.SetFloat(ScintGrain, profile.ScintillationScale.Value);
        material.SetFloat(ScintSpeed, profile.ScintillationSpeed.Value);

        // Identity: only the pre-upscale composite modes (0 and 1) would need a UV correction here.
        material.SetVector(ScreenUvScale, new Vector4(1f, 1f, 0f, 0f));
        material.SetVector(ScreenUvOffset, Vector4.zero);
    }

    // The rectangular zone in screen UV: the HUD's Display panel when it has one, otherwise the
    // profile's own size and offset. Either way the inset is applied last.
    private static Vector4 ZoneRectangle(ThermalDeviceProfile profile, float aspect)
    {
        // One canvas unit as a fraction of the screen's height and of its width.
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

    // Clips the thermal to the goggle's own tube mask - the same texture BSG cuts the night vision with.
    private static void ApplyTubeMask(Material material, ThermalDeviceProfile profile, NightVision nightVision, float aspect)
    {
        if (!profile.ClipToTubes.Value)
        {
            material.SetFloat(TubeClip, 0f);
            return;
        }

        var textureMask = nightVision.TextureMask;
        var mask = textureMask != null ? textureMask.Mask : null;

        if (!mask)
        {
            // Clipping to nothing would erase the overlay, so fail open.
            material.SetFloat(TubeClip, 0f);

            if (_loggedMissingTubeMask) { return; }

            _loggedMissingTubeMask = true;
            Plugin.logger!.LogWarning($"[WTT-CAG] {profile.Name}: no TextureMask.Mask on this NightVision - tube clipping disabled.");

            return;
        }

        var size = textureMask!.Size * profile.TubeMaskScale.Value;

        if (size < 0.0001f) { size = 1f; }

        material.SetFloat(TubeClip, 1f);
        material.SetTexture(TubeMask, mask);
        material.SetFloat(TubeInvMaskSize, 1f / size);
        material.SetFloat(TubeInvAspect, (float)mask!.height / mask.width);
        material.SetFloat(TubeCameraAspect, aspect);
        material.SetFloat(TubeMaskSoftness, profile.TubeMaskSoftness.Value);
        material.SetFloat(TubeMaskChannel, profile.TubeMaskChannel.Value);

        // Always inverted: BSG's masks mark the surround rather than the tube.
        material.SetFloat(TubeMaskInvert, 1f);
    }
}
