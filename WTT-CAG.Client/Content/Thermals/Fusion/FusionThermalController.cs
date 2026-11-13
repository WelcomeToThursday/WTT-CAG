using System;
using BSG.CameraEffects;
using UnityEngine;
using WTT_CAG.Client.Config;
using WTT_CAG.Client.Core.Thermal;

namespace WTT_CAG.Client.Content.Thermals.Fusion;

// Added to the FPS camera while a fusion goggle is worn. Owns the composite material, the thermal
// zone camera and both compositing paths, and tears all of it down when the goggle comes off.
public class FusionThermalController : MonoBehaviour
{
    private const int PostUpscaleCompositeMode = 2;

    private NightVision? _nightVision;
    private FusionThermalZoneCamera? _zoneCamera;
    private FusionThermalOverlayCamera? _overlayCamera;
    private FusionThermalHud? _hud;
    private ThermalDeviceProfile? _hudProfile;
    private bool _hudFailed;
    private Camera? _hostCamera;

    public FusionThermalOverlay? Overlay { get; private set; }

    private static bool PostUpscale => ThermalConfigSection.CompositeMode.Value == PostUpscaleCompositeMode;

    // The thermal channel only runs while the tube is on, like a clip-on module on a running tube.
    private bool ZoneWanted
    {
        get
        {
            var profile = ThermalDeviceProfiles.Active;

            return profile != null && profile.ZoneEnabled.Value && FusionThermalState.ThermalOn && _nightVision && _nightVision!.On;
        }
    }

    private void Awake()
    {
        _nightVision = GetComponent<NightVision>();

        Overlay = gameObject.AddComponent<FusionThermalOverlay>();
        Overlay.HostNVG = _nightVision;

        if (ThermalAssets.FusionMaterial)
        {
            Overlay.OverlayMaterial = new Material(ThermalAssets.FusionMaterial) { name = $"{ThermalAssets.FusionMaterial!.name}_{name}_Instance" };
        }
        else
        {
            Plugin.logger!.LogWarning("[WTT-CAG] No fusion material - the goggles will look stock.");
        }

        var hostCamera = GetComponent<Camera>();
        _hostCamera = hostCamera;

        if (!hostCamera)
        {
            Plugin.logger!.LogWarning($"[WTT-CAG] {name} has no Camera - cannot build the thermal zone.");
            return;
        }

        // ThermalVision.Awake dereferences several serialized fields without null checks. If either
        // camera fails to build, carry on with plain night vision rather than breaking the goggles.
        try
        {
            _zoneCamera = FusionThermalZoneCamera.Create(hostCamera);

            if (_zoneCamera) { _zoneCamera!.SetActive(ZoneWanted); }
        }
        catch (Exception e)
        {
            Plugin.logger!.LogError($"[WTT-CAG] Thermal zone camera failed to build: {e}");
            _zoneCamera = null;
        }

        if (!Overlay.OverlayMaterial) { return; }

        try
        {
            // Built regardless of composite mode and switched with it, so changing mode in F12 needs no rebuild.
            _overlayCamera = FusionThermalOverlayCamera.Create(hostCamera, Overlay.OverlayMaterial!);

            if (_overlayCamera) { _overlayCamera!.SetCameraEnabled(PostUpscale); }
        }
        catch (Exception e)
        {
            Plugin.logger!.LogError($"[WTT-CAG] Overlay camera failed to build: {e}");
            _overlayCamera = null;
        }
    }

    private void Update()
    {
        // Tube state, the zone toggle and the composite mode are all live.
        var wanted = ZoneWanted;

        if (_zoneCamera && _zoneCamera!.IsActive != wanted)
        {
            _zoneCamera.SetActive(wanted);
        }

        var tubeOn = _nightVision && _nightVision!.On;

        if (tubeOn) { FusionThermalState.AddRunTime(Time.deltaTime); }

        UpdateHud(tubeOn);

        if (!_overlayCamera) { return; }

        // The overlay camera follows the tube, not the thermal channel: the device overlay stays
        // up when thermal is switched off.
        _overlayCamera!.Active = tubeOn;
        _overlayCamera.SetCameraEnabled(PostUpscale);
    }

    // The HUD belongs to one goggle, so it is rebuilt if the worn goggle changes under us.
    private void UpdateHud(bool tubeOn)
    {
        var profile = ThermalDeviceProfiles.Active;

        if (_hud && _hud!.Profile != profile)
        {
            Destroy(_hud.gameObject);
            _hud = null;
        }

        if (!_hud && profile != null && _hostCamera && !_hudFailed)
        {
            try
            {
                _hud = FusionThermalHud.Create(_hostCamera!, profile);
            }
            catch (Exception e)
            {
                Plugin.logger!.LogError($"[WTT-CAG] HUD failed to build: {e}");
            }

            // One attempt per goggle: a missing prefab should not be retried every frame.
            _hudFailed = !_hud;
            _hudProfile = profile;
        }
        else if (profile != _hudProfile)
        {
            _hudFailed = false;
            _hudProfile = profile;
        }

        if (_hud) { _hud!.SetVisible(tubeOn); }
    }

    private void OnDestroy()
    {
        // Both cameras are parented to the FPS camera, which outlives this component, so they
        // have to be destroyed explicitly.
        if (_zoneCamera)
        {
            Destroy(_zoneCamera!.gameObject);
            _zoneCamera = null;
        }

        if (_overlayCamera)
        {
            Destroy(_overlayCamera!.gameObject);
            _overlayCamera = null;
        }

        if (_hud)
        {
            Destroy(_hud!.gameObject);
            _hud = null;
        }

        if (Overlay)
        {
            if (Overlay!.OverlayMaterial) { Destroy(Overlay.OverlayMaterial); }

            Destroy(Overlay);
            Overlay = null;
        }

        Plugin.logger!.LogInfo($"[WTT-CAG] Fusion thermal torn down on {name}.");
    }
}
