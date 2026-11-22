using System;
using BSG.CameraEffects;
using UnityEngine;
using WTT_CAG.Client.Config;
using WTT_CAG.Client.Core.Thermal;
using WTT_CAG.Client.Core;

namespace WTT_CAG.Client.Content.Thermals.Fusion;

// Added to the FPS camera while a fusion goggle is worn; owns the zone camera, HUD and composite paths.
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

    // The overlay camera is only used while the game upscales. With DLSS/FSR off, SSAAImpl draws the
    // final frame with a fixed-orientation mesh, and any extra camera rendering to the screen after it
    // flips the whole view. Without an upscaler the FPS camera is already full resolution, so the
    // composite is drawn there instead.
    public static bool PostUpscaleActive { get; private set; }

    private string? _lastPathLog;

    private bool GameIsUpscaling(out int renderWidth)
    {
        renderWidth = _hostCamera ? _hostCamera!.pixelWidth : Screen.width;

        return renderWidth < Screen.width * 0.99f;
    }

    private void UpdateCompositePath()
    {
        var upscaling = GameIsUpscaling(out var renderWidth);

        PostUpscaleActive = ThermalConfigSection.CompositeMode.Value == PostUpscaleCompositeMode && upscaling;

        var line = PostUpscaleActive
            ? $"Composite path: overlay camera - the game renders at {renderWidth} of {Screen.width} px and upscales."
            : $"Composite path: FPS camera - the game renders at {renderWidth} of {Screen.width} px, no upscaling.";

        if (line == _lastPathLog) { return; }

        _lastPathLog = line;
        CagLog.Info($"[WTT-CAG] {line}");
    }

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

        try
        {
            if (ThermalConfigSection.Diag(ThermalConfigSection.DiagSkipThermalCamera))
            {
                Plugin.logger!.LogWarning("[WTT-CAG] Diagnostics: thermal camera skipped.");
                throw new SkippedForDiagnosticsException();
            }

            _zoneCamera = FusionThermalZoneCamera.Create(hostCamera);

            if (_zoneCamera) { _zoneCamera!.SetActive(ZoneWanted); }
        }
        catch (SkippedForDiagnosticsException)
        {
            _zoneCamera = null;
        }
        catch (Exception e)
        {
            Plugin.logger!.LogError($"[WTT-CAG] Thermal zone camera failed to build: {e}");
            _zoneCamera = null;
        }

        if (!Overlay.OverlayMaterial) { return; }

        if (ThermalConfigSection.Diag(ThermalConfigSection.DiagSkipOverlayCamera))
        {
            Plugin.logger!.LogWarning("[WTT-CAG] Diagnostics: overlay camera skipped.");
            return;
        }

        try
        {
            _overlayCamera = FusionThermalOverlayCamera.Create(hostCamera, Overlay.OverlayMaterial!);

            UpdateCompositePath();

            if (_overlayCamera) { _overlayCamera!.SetCameraEnabled(PostUpscaleActive); }
        }
        catch (Exception e)
        {
            Plugin.logger!.LogError($"[WTT-CAG] Overlay camera failed to build: {e}");
            _overlayCamera = null;
        }
    }

    private void Update()
    {
        var wanted = ZoneWanted;

        if (_zoneCamera && _zoneCamera!.IsActive != wanted)
        {
            _zoneCamera.SetActive(wanted);
        }

        var zoomProfile = ThermalDeviceProfiles.Active;

        if (_zoneCamera && zoomProfile != null)
        {
            _zoneCamera!.SetPointFiltering(FusionThermalState.ZoomFactor(zoomProfile) > 1.01f);
        }

        UpdateCompositePath();

        var tubeOn = _nightVision && _nightVision!.On;

        var activeProfile = ThermalDeviceProfiles.Active;

        if (tubeOn && activeProfile != null) { FusionThermalState.AddRunTime(activeProfile, Time.deltaTime); }

        UpdateHud(tubeOn);

        if (!_overlayCamera) { return; }

        _overlayCamera!.Active = tubeOn;
        _overlayCamera.SetCameraEnabled(PostUpscaleActive);
    }

    private void UpdateHud(bool tubeOn)
    {
        var profile = ThermalDeviceProfiles.Active;

        if (_hud && _hud!.Profile != profile)
        {
            Destroy(_hud.gameObject);
            _hud = null;
        }

        if (!_hud && profile != null && _hostCamera && !_hudFailed && ThermalConfigSection.Diag(ThermalConfigSection.DiagSkipHud))
        {
            Plugin.logger!.LogWarning("[WTT-CAG] Diagnostics: HUD skipped.");
            _hudFailed = true;
            _hudProfile = profile;
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

            _hudFailed = !_hud;
            _hudProfile = profile;
        }
        else if (profile != _hudProfile)
        {
            _hudFailed = false;
            _hudProfile = profile;
        }

        if (_hud) { _hud!.SetVisible(tubeOn && (profile == null || !profile.HudNeedsThermal || FusionThermalState.ThermalOn)); }
    }

    private void OnDestroy()
    {
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

        CagLog.Info($"[WTT-CAG] Fusion thermal torn down on {name}.");
    }
}

internal sealed class SkippedForDiagnosticsException : Exception
{
}
