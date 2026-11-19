using WTT_CAG.Client.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using WTT_CAG.Client.Config;
using WTT_CAG.Client.Core.Thermal;
using WTT_CAG.Client.Patches.ThermalVisionPatches;

namespace WTT_CAG.Client.Content.Thermals.Fusion;

// Second camera rendering the scene as thermal into a texture that FusionNVG composites into the zones.
public class FusionThermalZoneCamera : MonoBehaviour
{
    private const string ThermalRtGlobalName = "_ThermalZoneRT";

    public const string CameraObjectName = "WTT_CAG_FusionThermalZoneCamera";

    private const float DepthOffset = -10f;

    private Camera? _hostCamera;
    private Camera? _thermalCamera;

    private ThermalVision? _hostThermal;
    private ThermalVision? _thermalVision;
    private Material? _traceMaterial;
    private RenderTexture? _thermalRt;
    private int _rtWidth, _rtHeight;
    private string? _lastExcludeLayers;

    // With HoldZoomWhenAiming on: the widest host FOV seen (hip-fire), so aiming does not magnify the thermal.
    private float _referenceHostFov = float.NaN;
    private bool _loggedViewportReset;

    private static readonly int ThermalVisionOnID = Shader.PropertyToID("_ThermalVisionOn");
    private static readonly int ThermalRtID = Shader.PropertyToID(ThermalRtGlobalName);

    private static ThermalDeviceProfile? DeviceProfile => ThermalDeviceProfiles.Active;

    private static float ZoneFOV => DeviceProfile?.Fov.Value ?? 40f;
    private static int ZoneResolution => DeviceProfile?.Resolution.Value ?? 640;
    private static bool ZoneCullingEnabled => DeviceProfile?.CullLayers.Value ?? false;
    private static string ZoneExcludeLayers => DeviceProfile?.ExcludeLayers.Value ?? "";

    public static FusionThermalZoneCamera? Instance { get; private set; }

    // Debug builds log the numbers behind ZoneUVScale whenever they change.
    public static string ScaleReport
    {
        get
        {
            var self = Instance;

            if (!self || !self!._thermalCamera || !self._hostCamera) { return "no zone camera"; }

            return $"host FOV {self._hostCamera!.fieldOfView:F1} (held {self._referenceHostFov:F1}), zone FOV {self._thermalCamera!.fieldOfView:F1}, scale {ZoneUVScale:F3}";
        }
    }

    public static float ZoneUVScale
    {
        get
        {
            var self = Instance;

            if (!self || !self!._thermalCamera || !self._hostCamera) { return 1f; }

            var hostFov = float.IsNaN(self._referenceHostFov) ? self._hostCamera!.fieldOfView : self._referenceHostFov;
            var hostHalfRad = hostFov * 0.5f * Mathf.Deg2Rad;
            var zoneHalfRad = self._thermalCamera!.fieldOfView * 0.5f * Mathf.Deg2Rad;
            var zoneTangent = Mathf.Tan(zoneHalfRad);

            if (zoneTangent < 1e-4f) { return 1f; }

            return Mathf.Tan(hostHalfRad) / zoneTangent;
        }
    }

    public static FusionThermalZoneCamera? Create(Camera hostCamera)
    {
        if (!hostCamera)
        {
            Plugin.logger!.LogError("[WTT-CAG] Host camera is null.");
            return null;
        }

        var hostThermal = hostCamera.GetComponent<ThermalVision>();

        if (!hostThermal)
        {
            Plugin.logger!.LogError($"[WTT-CAG] {hostCamera.name} has no ThermalVision to copy settings from. Unable to build thermal camera.");
            return null;
        }

        var go = new GameObject(CameraObjectName);

        // Inactive until built: ThermalVision.Awake throws if its serialized fields are not set yet.
        go.SetActive(false);
        go.transform.SetParent(hostCamera.transform, false);

        var self = go.AddComponent<FusionThermalZoneCamera>();
        self._hostCamera = hostCamera;
        self._hostThermal = hostThermal;
        self.Build();

        return self;
    }

    private void Build()
    {
        _thermalCamera = gameObject.AddComponent<Camera>();
        _thermalCamera.CopyFrom(_hostCamera);

        _thermalCamera.RemoveAllCommandBuffers();
        _thermalCamera.depth = _hostCamera!.depth + DepthOffset;
        _thermalCamera.clearFlags = CameraClearFlags.SolidColor;
        _thermalCamera.backgroundColor = Color.black;
        _thermalCamera.fieldOfView = Mathf.Clamp(ZoneFOV, 5f, 90f);
        _thermalCamera.allowHDR = false;
        _thermalCamera.allowMSAA = false;

        _thermalCamera.allowDynamicResolution = false;

        // SSAAImpl shrinks the FPS camera's rect while upscaling, and CopyFrom copies it.
        ResetViewport();

        ApplyCullingMask();
        AllocateRenderTexture();

        if (ThermalConfigSection.Diag(ThermalConfigSection.DiagSkipThermalCameraExtras))
        {
            Plugin.logger!.LogWarning("[WTT-CAG] Diagnostics: thermal camera built without VolumetricLightRenderer and ChromaticAberration.");
        }
        else
        {
            // ThermalVision dereferences both of these without null checks.
            if (!gameObject.GetComponent<ChromaticAberration>())
            {
                gameObject.AddComponent<ChromaticAberration>();
            }

            if (!gameObject.GetComponent<VolumetricLightRenderer>())
            {
                gameObject.AddComponent<VolumetricLightRenderer>();
            }
        }

        _thermalVision = gameObject.AddComponent<ThermalVision>();

        _thermalVision.BlackFlashGoingToOn = _hostThermal!.BlackFlashGoingToOn;
        _thermalVision.BlackFlashGoingToOff = _hostThermal.BlackFlashGoingToOff;

        CloneUtility("ThermalVisionUtilities");
        CloneUtility("StuckFpsUtilities");
        CloneUtility("MotionBlurUtilities");
        CloneUtility("GlitchUtilities");
        CloneUtility("PixelationUtilities");

        // No mask here; the host masks the final image. Never call StartSwitch on this instance.
        _thermalVision.TextureMask = null;

        _thermalVision.SwitchComponentsOn = [];
        _thermalVision.SwitchComponentsOff = [];

        ApplySensorEffects();

        Instance = this;

        gameObject.SetActive(true);

        RegisterTraceMaterial();

        _thermalVision.On = true;

        CagLog.Info($"[WTT-CAG] Built zone on {_hostCamera.name} | dimensions: {_rtWidth}x{_rtHeight} | fov: {_thermalCamera.fieldOfView} | depth: {_thermalCamera.depth} (host: {_hostCamera.depth})");
    }

    private void RegisterTraceMaterial()
    {
        if (!ThermalAssets.TraceMaterial)
        {
            Plugin.logger!.LogWarning("[WTT-CAG] No trace material - the thermal zone will use the stock thermal look.");
            return;
        }

        _traceMaterial = new Material(ThermalAssets.TraceMaterial) { name = $"{ThermalAssets.TraceMaterial!.name}_{name}_Instance" };

        SetMaterialPropertiesPatch.Register(_thermalVision!, _traceMaterial, EThermalDeviceType.FUSION_THERMAL_ZONE);
    }

    private void ResetViewport()
    {
        _thermalCamera!.rect = new Rect(0f, 0f, 1f, 1f);
        _thermalCamera.ResetProjectionMatrix();
    }

    private static bool IsFullViewport(Rect rect)
    {
        return Mathf.Abs(rect.x) < 0.0001f && Mathf.Abs(rect.y) < 0.0001f && Mathf.Abs(rect.width - 1f) < 0.0001f && Mathf.Abs(rect.height - 1f) < 0.0001f;
    }

    private void CloneUtility(string fieldName)
    {
        try
        {
            var field = typeof(ThermalVision).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                return;
            }

            var source = field.GetValue(_hostThermal);
            if (source == null)
            {
                return;
            }

            var copy = source is UnityEngine.Object unityObject ? Instantiate(unityObject) : source;

            field.SetValue(_thermalVision, copy);
        }
        catch (Exception e)
        {
            Plugin.logger!.LogWarning($"[WTT-CAG] Could not copy {fieldName}: {e.GetType().Name}");
        }
    }

    private void ApplyCullingMask()
    {
        if (!ZoneCullingEnabled)
        {
            _thermalCamera!.cullingMask = _hostCamera!.cullingMask;

            CagLog.Info("[WTT-CAG] Culling disabled - rendering all host layers.");

            return;
        }

        var mask = _hostCamera!.cullingMask;
        var excluded = new List<string>();
        var unknown = new List<string>();

        foreach (var name in ZoneExcludeLayers.Split(','))
        {
            var trimmed = name.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            var layer = LayerMask.NameToLayer(trimmed);

            if (layer < 0)
            {
                unknown.Add(trimmed);
                continue;
            }

            mask &= ~(1 << layer);
            excluded.Add($"{trimmed}({layer})");
        }

        _thermalCamera!.cullingMask = mask;

        CagLog.Info($"[WTT-CAG] Culling excluded: [{string.Join(", ", excluded)}]");

        if (unknown.Count == 0) { return; }

        var present = new List<string>();

        for (var i = 0; i < 32; i++)
        {
            var name = LayerMask.LayerToName(i);

            if (!string.IsNullOrEmpty(name)) { present.Add($"{i}:{name}"); }
        }

        Plugin.logger!.LogWarning($"[WTT-CAG] Culling layers not found: [{string.Join(", ", unknown)}]. Layers in this build: {string.Join(", ", present)}");
    }

    private void ApplySensorEffects()
    {
        if (!_thermalVision)
        {
            return;
        }

        var profile = ThermalDeviceProfiles.Active;

        if (profile == null)
        {
            return;
        }

        _thermalVision!.IsNoisy = profile.FxNoise.Value;
        _thermalVision.IsFpsStuck = profile.FxFpsStuck.Value;
        _thermalVision.IsMotionBlurred = profile.FxMotionBlur.Value;
        _thermalVision.IsGlitch = profile.FxGlitch.Value;
        _thermalVision.IsPixelated = profile.FxPixelate.Value;
        _thermalVision.UnsharpRadiusBlur = profile.UnsharpRadius.Value;
        _thermalVision.UnsharpBias = profile.UnsharpBias.Value;
    }

    private void AllocateRenderTexture()
    {
        var width = Mathf.Clamp(ZoneResolution, 128, 1024);
        var height = Mathf.RoundToInt(width / Mathf.Max(0.1f, _hostCamera!.aspect));

        if (_thermalRt && _rtWidth == width && _rtHeight == height)
        {
            return;
        }

        ReleaseRenderTexture();

        _thermalRt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
        {
            name = "CAG_ThermalZoneRT",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false
        };

        _thermalRt.Create();

        _rtWidth = width;
        _rtHeight = height;

        _thermalCamera!.targetTexture = _thermalRt;

        Shader.SetGlobalTexture(ThermalRtID, _thermalRt);
    }

    private void ReleaseRenderTexture()
    {
        if (!_thermalRt)
        {
            return;
        }

        if (_thermalCamera && _thermalCamera!.targetTexture == _thermalRt)
        {
            _thermalCamera.targetTexture = null;
        }

        _thermalRt!.Release();
        Destroy(_thermalRt);
        _thermalRt = null;
    }

    private void LateUpdate()
    {
        if (!_hostCamera || !_thermalCamera)
        {
            return;
        }

        if (!IsFullViewport(_thermalCamera!.rect))
        {
            if (!_loggedViewportReset)
            {
                _loggedViewportReset = true;
                Plugin.logger!.LogWarning($"[WTT-CAG] Thermal camera viewport was changed to {_thermalCamera.rect} after it was built - resetting it every frame.");
            }

            ResetViewport();
        }

        transform.position = _hostCamera!.transform.position;
        transform.rotation = _hostCamera!.transform.rotation;

        if (!Mathf.Approximately(_thermalCamera!.aspect, _hostCamera.aspect))
        {
            _thermalCamera.aspect = _hostCamera.aspect;
        }

        var hostFov = _hostCamera.fieldOfView;
        var holdZoom = DeviceProfile?.HoldZoomWhenAiming.Value ?? true;
        _referenceHostFov = holdZoom && !float.IsNaN(_referenceHostFov) ? Mathf.Max(_referenceHostFov, hostFov) : hostFov;

        var desiredFov = Mathf.Clamp(ZoneFOV, 5f, 90f);

        if (!Mathf.Approximately(_thermalCamera.fieldOfView, desiredFov))
        {
            _thermalCamera.fieldOfView = desiredFov;
        }

        ApplySensorEffects();

        var cullingKey = $"{ZoneCullingEnabled}|{ZoneExcludeLayers}";

        if (_lastExcludeLayers != cullingKey)
        {
            _lastExcludeLayers = cullingKey;
            ApplyCullingMask();
        }

        var desiredWidth = Mathf.Clamp(ZoneResolution, 128, 1024);

        if (desiredWidth != _rtWidth)
        {
            AllocateRenderTexture();
        }
    }

    public void SetActive(bool on)
    {
        if (!_thermalCamera) { return; }
        if (_thermalCamera!.enabled == on) { return; }

        _thermalCamera.enabled = on;

        if (on) { return; }

        // Disabling between ThermalVision's OnPreCull and OnPostRender would leave the whole game rendering as heat.
        Shader.SetGlobalFloat(ThermalVisionOnID, 0f);
        CagLog.Info("[WTT-CAG] Thermal zone camera disabled.");
    }

    public bool IsActive => _thermalCamera && _thermalCamera!.enabled;

    public RenderTexture? ThermalTexture => _thermalRt;

    private void OnDestroy()
    {
        if (_thermalVision)
        {
            SetMaterialPropertiesPatch.Unregister(_thermalVision!);
        }

        if (_traceMaterial)
        {
            Destroy(_traceMaterial);
            _traceMaterial = null;
        }

        if (Instance == this)
        {
            Instance = null;
        }

        ReleaseRenderTexture();
        Shader.SetGlobalTexture(ThermalRtID, Texture2D.blackTexture);

        Shader.SetGlobalFloat(ThermalVisionOnID, 0f);

        CagLog.Info($"[WTT-CAG] Destroyed {name}");
    }
}