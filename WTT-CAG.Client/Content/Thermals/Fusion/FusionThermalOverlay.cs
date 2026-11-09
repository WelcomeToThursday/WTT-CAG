using System.Reflection;
using BSG.CameraEffects;
using UnityEngine;
using UnityEngine.Rendering;
using WTT_CAG.Client.Config;

namespace WTT_CAG.Client.Content.Thermals.Fusion;

public class FusionThermalOverlay : MonoBehaviour
{
    public Material? OverlayMaterial;
    public NightVision? HostNVG;

    private Camera? _camera;
    private CommandBuffer? _commandBuffer;

    private static readonly FieldInfo? _ssaaField = typeof(NightVision).GetField("_ssaa", BindingFlags.NonPublic | BindingFlags.Instance);

    private static MethodInfo? _getRtIdentifier;
    private static bool? _rtIdentifierLookupFailed;
    
    private string? _lastLoggedState;
    private bool? _usingSSAATarget;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        
        _commandBuffer = new CommandBuffer { name = "Fusion Zone Composite" };

        if (!_camera) { return; }

        _camera.RemoveCommandBuffer(CameraEvent.AfterImageEffects, _commandBuffer);
        _camera.AddCommandBuffer(CameraEvent.AfterImageEffects, _commandBuffer);
    }

    public void RebuildCommandBuffer()
    {
        if (_commandBuffer == null) { return; }

        _commandBuffer.Clear();

        if (!OverlayMaterial || !HostNVG || !HostNVG!.On)
        {
            LogStateOnce($"[WTT-CAG] Idle - material: {OverlayMaterial} host: {HostNVG} on: {HostNVG?.On}");
            return;
        }

        var profile = ThermalDeviceProfiles.Active;

        if (profile == null || !profile.ZoneEnabled!.Value)
        {
            if (profile == null)
            {
                LogStateOnce("[WTT-CAG] Idle - no device equipped.");
                return;
            }
            
            LogStateOnce("[WTT-CAG] Idle - device zone disabled.");
            return;
        }

        var target = ResolveTargetIdentifier();
        
        _commandBuffer.Blit(Texture2D.blackTexture, target, OverlayMaterial);
        
        LogStateOnce($"[WTT-CAG] Compositing - event: AfterImageEffects ssaaTarget: {_usingSSAATarget} zoneRt: {FusionThermalZoneCamera.Instance?.ThermalTexture != null}");
    }

    private RenderTargetIdentifier ResolveTargetIdentifier()
    {
        if (ThermalConfigSection.CompositeMode!.Value != 0)
        {
            _usingSSAATarget = false;
            return BuiltinRenderTextureType.CameraTarget;
        }

        if (_ssaaField != null && HostNVG && _rtIdentifierLookupFailed != true)
        {
            try
            {
                var ssaa = _ssaaField.GetValue(HostNVG);

                if (ssaa != null)
                {
                    if (_getRtIdentifier == null)
                    {
                        _getRtIdentifier = ssaa.GetType().GetMethod("GetRTIdentifier", BindingFlags.Instance);

                        if (_getRtIdentifier == null)
                        {
                            _rtIdentifierLookupFailed = true;
                            Plugin.logger!.LogWarning("[WTT-CAG] SSAA.GetRTIdentifier not found - falling back to CameraTarget. Expect a flipped or mis-scaled composite if supersampling is enabled.");
                            return BuiltinRenderTextureType.CameraTarget;
                        }
                    }

                    _usingSSAATarget = true;
                    return (RenderTargetIdentifier)_getRtIdentifier.Invoke(ssaa, null);
                }
            }
            catch
            {
                // TODO: nothing. Absolutely nothing here.
            }
        }
        _usingSSAATarget = false;
        return BuiltinRenderTextureType.CameraTarget;
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
