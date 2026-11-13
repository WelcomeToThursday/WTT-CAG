using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using WTT_CAG.Client.Core.Thermal;
using WTT_CAG.Client.Patches.ThermalVisionPatches;

namespace WTT_CAG.Client.Content.Thermals.Fusion;

// A second, low-resolution camera that renders the scene as thermal into a RenderTexture, which the
// FusionNVG material then composites into the goggles' thermal zones. It has to be a separate
// camera: while a ThermalVision is on, everything its camera renders is heat data, so there would
// be no night vision image left to show outside the zones.
public class FusionThermalZoneCamera : MonoBehaviour
{
    private const string ThermalRtGlobalName = "_ThermalZoneRT";

    public const string CameraObjectName = "WTT_CAG_FusionThermalZoneCamera";

    // Negative so this camera renders, and clears _ThermalVisionOn, before the FPS camera starts.
    private const float DepthOffset = -10f;

    private Camera? _hostCamera;
    private Camera? _thermalCamera;

    private ThermalVision? _hostThermal;
    private ThermalVision? _thermalVision;
    private Material? _traceMaterial;
    private RenderTexture? _thermalRt;
    private int _rtWidth, _rtHeight;
    private string? _lastExcludeLayers;

    private static readonly int ThermalVisionOnID = Shader.PropertyToID("_ThermalVisionOn");
    private static readonly int ThermalRtID = Shader.PropertyToID(ThermalRtGlobalName);

    private static ThermalDeviceProfile? DeviceProfile => ThermalDeviceProfiles.Active;

    private static float ZoneFOV => DeviceProfile?.Fov.Value ?? 40f;
    private static int ZoneResolution => DeviceProfile?.Resolution.Value ?? 640;
    private static bool ZoneCullingEnabled => DeviceProfile?.CullLayers.Value ?? false;
    private static string ZoneExcludeLayers => DeviceProfile?.ExcludeLayers.Value ?? "";

    public static FusionThermalZoneCamera? Instance { get; private set; }

    // How much the zone magnifies relative to the host view: tan(hostHalfFov) / tan(zoneHalfFov).
    public static float ZoneUVScale
    {
        get
        {
            var self = Instance;

            if (!self || !self!._thermalCamera || !self._hostCamera) { return 1f; }

            var hostHalfRad = self._hostCamera!.fieldOfView * 0.5f * Mathf.Deg2Rad;
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

        // ThermalVision has serialized fields AddComponent cannot populate, so they are borrowed from the host's.
        var hostThermal = hostCamera.GetComponent<ThermalVision>();

        if (!hostThermal)
        {
            Plugin.logger!.LogError($"[WTT-CAG] {hostCamera.name} has no ThermalVision to copy settings from. Unable to build thermal camera.");
            return null;
        }

        var go = new GameObject(CameraObjectName);

        // Inactive first: AddComponent runs Awake immediately on an active object, and
        // ThermalVision.Awake throws if its serialized fields have not been assigned yet.
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

        // The copy brings the host's full-screen DLSS/SSAA/post-processing buffers across.
        _thermalCamera.RemoveAllCommandBuffers();
        _thermalCamera.depth = _hostCamera!.depth + DepthOffset;
        _thermalCamera.clearFlags = CameraClearFlags.SolidColor;
        _thermalCamera.backgroundColor = Color.black;
        _thermalCamera.fieldOfView = Mathf.Clamp(ZoneFOV, 5f, 90f);
        _thermalCamera.allowHDR = false;
        _thermalCamera.allowMSAA = false;

        ApplyCullingMask();
        AllocateRenderTexture();

        // ThermalVision dereferences both of these without null checks.
        if (!gameObject.GetComponent<ChromaticAberration>())
        {
            gameObject.AddComponent<ChromaticAberration>();
        }

        if (!gameObject.GetComponent<VolumetricLightRenderer>())
        {
            gameObject.AddComponent<VolumetricLightRenderer>();
        }

        _thermalVision = gameObject.AddComponent<ThermalVision>();

        _thermalVision.BlackFlashGoingToOn = _hostThermal!.BlackFlashGoingToOn;
        _thermalVision.BlackFlashGoingToOff = _hostThermal.BlackFlashGoingToOff;

        // Cloned, so per-goggle tuning cannot leak onto the FPS camera's own thermal.
        CloneUtility("ThermalVisionUtilities");
        CloneUtility("StuckFpsUtilities");
        CloneUtility("MotionBlurUtilities");
        CloneUtility("GlitchUtilities");
        CloneUtility("PixelationUtilities");

        // No mask here - the host camera masks the final image. Never call StartSwitch/Toggle on
        // this instance: the switcher dereferences TextureMask, which is why On is set directly.
        _thermalVision.TextureMask = null;

        _thermalVision.SwitchComponentsOn = [];
        _thermalVision.SwitchComponentsOff = [];

        ApplySensorEffects();

        Instance = this;

        gameObject.SetActive(true);

        RegisterTraceMaterial();

        // After activation: the On setter touches state that Awake builds.
        _thermalVision.On = true;

        Plugin.logger!.LogInfo($"[WTT-CAG] Built zone on {_hostCamera.name} | dimensions: {_rtWidth}x{_rtHeight} | fov: {_thermalCamera.fieldOfView} | depth: {_thermalCamera.depth} (host: {_hostCamera.depth})");
    }

    // Gives this camera's ThermalVision the ThermalTraceOverlay look. Without it the zone renders
    // with the game's stock white-hot material.
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

            Plugin.logger!.LogInfo("[WTT-CAG] Culling disabled - rendering all host layers.");

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

        Plugin.logger!.LogInfo($"[WTT-CAG] Culling excluded: [{string.Join(", ", excluded)}]");

        if (unknown.Count == 0) { return; }

        // Layer names are build-specific, so list what actually exists.
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

    // LateUpdate so this runs after the procedural weapon animation has moved the FPS camera.
    private void LateUpdate()
    {
        if (!_hostCamera || !_thermalCamera)
        {
            return;
        }

        transform.position = _hostCamera!.transform.position;
        transform.rotation = _hostCamera!.transform.rotation;

        if (!Mathf.Approximately(_thermalCamera!.aspect, _hostCamera.aspect))
        {
            _thermalCamera.aspect = _hostCamera.aspect;
        }

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

        // ThermalVision sets this in OnPreCull and clears it in OnPostRender. Disabling the camera
        // between the two would leave the whole game rendering as heat.
        Shader.SetGlobalFloat(ThermalVisionOnID, 0f);
        Plugin.logger!.LogInfo("[WTT-CAG] Thermal zone camera disabled.");
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

        Plugin.logger!.LogInfo($"[WTT-CAG] Destroyed {name}");
    }
}
