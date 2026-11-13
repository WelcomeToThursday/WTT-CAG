using System.Reflection;
using BSG.CameraEffects;
using UnityEngine;
using UnityEngine.Rendering;
using WTT_CAG.Client.Config;

namespace WTT_CAG.Client.Content.Thermals.Fusion;

// The pre-upscale composite, used by composite modes 0 and 1. A CommandBuffer rather than
// OnRenderImage: an image effect on the FPS camera fights SSAA's render target.
public class FusionThermalOverlay : MonoBehaviour
{
    private const int PostUpscaleCompositeMode = 2;

    public Material? OverlayMaterial;
    public NightVision? HostNVG;

    private Camera? _camera;
    private CommandBuffer? _commandBuffer;

    private static readonly FieldInfo? _ssaaField = FindSSAAField();

    private static MethodInfo? _getRtIdentifier;
    private static bool _rtIdentifierLookupFailed;

    private string? _lastLoggedState;
    private bool _usingSSAATarget;

    private static FieldInfo? FindSSAAField()
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        // The field name differs between game builds, so take whichever exists.
        return typeof(NightVision).GetField("_ssaa", flags) ?? typeof(NightVision).GetField("ssaa_0", flags);
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>();

        _commandBuffer = new CommandBuffer { name = "Fusion Zone Composite" };

        if (!_camera) { return; }

        // AfterImageEffects is strictly later than NightVision's own BeforeImageEffects blit,
        // so this always lands on the finished night vision image.
        _camera.RemoveCommandBuffer(CameraEvent.AfterImageEffects, _commandBuffer);
        _camera.AddCommandBuffer(CameraEvent.AfterImageEffects, _commandBuffer);
    }

    public void RebuildCommandBuffer()
    {
        if (_commandBuffer == null) { return; }

        _commandBuffer.Clear();

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

        // FusionThermalOverlayCamera draws the composite in this mode. Doing both would double the overlay.
        if (ThermalConfigSection.CompositeMode.Value == PostUpscaleCompositeMode)
        {
            LogStateOnce("Idle - overlay camera owns the composite.");
            return;
        }

        var target = ResolveTargetIdentifier();

        // Blit rather than SetRenderTarget + DrawMesh: Blit takes its viewport from the destination.
        _commandBuffer.Blit(Texture2D.blackTexture, target, OverlayMaterial);

        LogStateOnce($"Compositing - event: AfterImageEffects ssaaTarget: {_usingSSAATarget} zoneRt: {FusionThermalZoneCamera.Instance?.ThermalTexture != null}");
    }

    private RenderTargetIdentifier ResolveTargetIdentifier()
    {
        _usingSSAATarget = false;

        // Only mode 0 draws into the SSAA buffer.
        if (ThermalConfigSection.CompositeMode.Value != 0 || _ssaaField == null || !HostNVG || _rtIdentifierLookupFailed)
        {
            return BuiltinRenderTextureType.CameraTarget;
        }

        try
        {
            var ssaa = _ssaaField.GetValue(HostNVG);

            // Null when supersampling is off, in which case the camera target is correct.
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
            // Reflection failed - the camera target is the same fallback NightVision itself uses.
            return BuiltinRenderTextureType.CameraTarget;
        }
    }

    private void LogStateOnce(string state)
    {
        if (state == _lastLoggedState) { return; }

        _lastLoggedState = state;

        Plugin.logger!.LogInfo($"[WTT-CAG] {state}");
    }

    private void OnDestroy()
    {
        if (_camera && _commandBuffer != null)
        {
            _camera!.RemoveCommandBuffer(CameraEvent.AfterImageEffects, _commandBuffer);
        }

        _commandBuffer?.Release();
        _commandBuffer = null;
    }
}
