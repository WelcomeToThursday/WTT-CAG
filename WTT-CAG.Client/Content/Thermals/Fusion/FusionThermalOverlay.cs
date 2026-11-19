using WTT_CAG.Client.Core;
using System.Reflection;
using BSG.CameraEffects;
using UnityEngine;
using UnityEngine.Rendering;
using WTT_CAG.Client.Config;

namespace WTT_CAG.Client.Content.Thermals.Fusion;

// Composite on the FPS camera itself, used whenever the overlay camera is not. Only attached while drawing.
public class FusionThermalOverlay : MonoBehaviour
{
    public Material? OverlayMaterial;
    public NightVision? HostNVG;

    private Camera? _camera;
    private CommandBuffer? _commandBuffer;

    private static readonly FieldInfo? _ssaaField = FindSSAAField();

    private static MethodInfo? _getRtIdentifier;
    private static bool _rtIdentifierLookupFailed;

    private string? _lastLoggedState;
    private bool _attached;
    private bool _usingSSAATarget;

    private static FieldInfo? FindSSAAField()
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        return typeof(NightVision).GetField("_ssaa", flags) ?? typeof(NightVision).GetField("ssaa_0", flags);
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>();

        _commandBuffer = new CommandBuffer { name = "Fusion Zone Composite" };
    }

    private void SetAttached(bool attach)
    {
        if (attach == _attached || !_camera || _commandBuffer == null) { return; }

        _attached = attach;

        if (attach)
        {
            _camera!.AddCommandBuffer(CameraEvent.AfterImageEffects, _commandBuffer);
        }
        else
        {
            _camera!.RemoveCommandBuffer(CameraEvent.AfterImageEffects, _commandBuffer);
        }
    }

    public void RebuildCommandBuffer()
    {
        if (_commandBuffer == null) { return; }

        _commandBuffer.Clear();

        SetAttached(!FusionThermalController.PostUpscaleActive);

        if (!OverlayMaterial || !HostNVG || !HostNVG!.On)
        {
            LogStateOnce($"Idle - material: {OverlayMaterial} host: {HostNVG} on: {HostNVG?.On}");
            return;
        }

        var profile = ThermalDeviceProfiles.Active;

        if (profile == null)
        {
            LogStateOnce("Idle - no device equipped.");
            return;
        }

        if (!profile.ZoneEnabled.Value)
        {
            LogStateOnce("Idle - device zone disabled.");
            return;
        }

        if (FusionThermalController.PostUpscaleActive)
        {
            LogStateOnce("Idle - overlay camera owns the composite.");
            return;
        }

        var target = ResolveTargetIdentifier();

        _commandBuffer.Blit(Texture2D.blackTexture, target, OverlayMaterial);

        LogStateOnce($"Compositing - event: AfterImageEffects ssaaTarget: {_usingSSAATarget} zoneRt: {FusionThermalZoneCamera.Instance?.ThermalTexture != null}");
    }

    private RenderTargetIdentifier ResolveTargetIdentifier()
    {
        _usingSSAATarget = false;

        if (ThermalConfigSection.CompositeMode.Value != 0 || _ssaaField == null || !HostNVG || _rtIdentifierLookupFailed)
        {
            return BuiltinRenderTextureType.CameraTarget;
        }

        try
        {
            var ssaa = _ssaaField.GetValue(HostNVG);

            if (ssaa == null)
            {
                return BuiltinRenderTextureType.CameraTarget;
            }

            _getRtIdentifier ??= ssaa.GetType().GetMethod("GetRTIdentifier", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (_getRtIdentifier == null)
            {
                _rtIdentifierLookupFailed = true;
                Plugin.logger!.LogWarning("[WTT-CAG] SSAA.GetRTIdentifier not found - falling back to CameraTarget. Expect a flipped or mis-scaled composite if supersampling is enabled.");
                return BuiltinRenderTextureType.CameraTarget;
            }

            var target = (RenderTargetIdentifier)_getRtIdentifier.Invoke(ssaa, null)!;

            _usingSSAATarget = true;

            return target;
        }
        catch
        {
            return BuiltinRenderTextureType.CameraTarget;
        }
    }

    private void LogStateOnce(string state)
    {
        if (state == _lastLoggedState) { return; }

        _lastLoggedState = state;

        CagLog.Info($"[WTT-CAG] {state}");
    }

    private void OnDestroy()
    {
        SetAttached(false);

        _commandBuffer?.Release();
        _commandBuffer = null;
    }
}
