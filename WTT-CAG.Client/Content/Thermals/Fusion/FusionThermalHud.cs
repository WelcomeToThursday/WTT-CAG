using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using UnityEngine;
using UnityEngine.UI;
using WTT_CAG.Client.Config;
using WTT_CAG.Client.Content.Thermals.Holosun;
using WTT_CAG.Client.Core.Thermal;
using WTT_CAG.Client.Core;

namespace WTT_CAG.Client.Content.Thermals.Fusion;

// Instantiates the HUD prefab and renders it into a screen-sized texture used as the device overlay.
public class FusionThermalHud : MonoBehaviour
{
    private const string CameraObjectName = "WTT_CAG_FusionThermalHudCamera";

    private const int HudLayer = 5;

    // Far below any map, so no other camera ever sees the canvas.
    private static readonly Vector3 ParkedPosition = new(0f, -5000f, 0f);

    private const float DepthOffset = -20f;

    private const float TapeStartHeading = 26f;

    private const float SlowUpdateInterval = 0.25f;
    private const float RtaScanInterval = 1f;

    private const string BlankClock = "--:--:--";

    private static readonly List<Mod> WeaponParts = [];

    public ThermalDeviceProfile? Profile { get; private set; }

    public static FusionThermalHud? Instance { get; private set; }

    private Camera? _hostCamera;
    private Camera? _hudCamera;
    private RenderTexture? _texture;

    public const float CanvasHeight = 1080f;

    private RectTransform? _display;
    private Vector2 _authoredPosition;
    private Vector2 _appliedOffset = new(float.NaN, float.NaN);
    private float _appliedScale = float.NaN;
    private readonly Vector3[] _corners = new Vector3[4];

    private RawImage? _compassTape;
    private Text? _heading;
    private Text? _missionTime;
    private Text? _mode;
    private Text? _rtaStatus;
    private Text? _runTime;
    private Text? _zoom;

    // The HUD camera only renders on frames where something on the HUD changed; the texture keeps
    // the last image in between.
    private bool _visible = true;
    private bool _dirty = true;
    private Graphic? _packetLoss;

    private float _lastTapeHeading = float.NaN;
    private int _lastHeadingDegrees = -1;
    private float _nextSlowUpdate;
    private float _nextRtaScan;
    private bool _rtaLinked;

    private float _nextPacketLossBurst;
    private int _packetLossBlinksLeft;
    private float _nextPacketLossBlink;

    public Texture? Texture => _texture;

    public static FusionThermalHud? Create(Camera hostCamera, ThermalDeviceProfile profile)
    {
        if (string.IsNullOrEmpty(profile.HudPrefab)) { return null; }

        var prefab = ThermalAssets.LoadHudPrefab(profile.HudPrefab);

        if (!prefab) { return null; }

        var go = new GameObject(CameraObjectName);
        go.transform.position = ParkedPosition;

        var self = go.AddComponent<FusionThermalHud>();
        self._hostCamera = hostCamera;
        self.Profile = profile;
        self.Build(prefab!);

        return self;
    }

    private void Build(GameObject prefab)
    {
        _hudCamera = gameObject.AddComponent<Camera>();
        _hudCamera.orthographic = true;
        _hudCamera.nearClipPlane = 0.1f;
        _hudCamera.farClipPlane = 10f;
        _hudCamera.cullingMask = 1 << HudLayer;
        _hudCamera.clearFlags = CameraClearFlags.SolidColor;

        // Transparent white, so the soft edges of the lettering do not darken.
        _hudCamera.backgroundColor = new Color(1f, 1f, 1f, 0f);
        _hudCamera.depth = _hostCamera!.depth + DepthOffset;
        _hudCamera.useOcclusionCulling = false;
        _hudCamera.allowHDR = false;
        _hudCamera.allowMSAA = false;

        AllocateTexture();

        var instance = Instantiate(prefab, transform, false);
        instance.name = prefab.name;

        foreach (var child in instance.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = HudLayer;
        }

        var canvas = instance.GetComponent<Canvas>();

        if (canvas)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _hudCamera;
            canvas.planeDistance = 1f;
        }
        else
        {
            Plugin.logger!.LogWarning($"[WTT-CAG] HUD prefab '{prefab.name}' has no Canvas on its root object - it will not draw.");
        }

        _display = Find<RectTransform>(instance, "Display");

        if (_display) { _authoredPosition = _display!.anchoredPosition; }
        _compassTape = Find<RawImage>(instance, "CompassTape");
        _heading = Find<Text>(instance, "Heading");
        _missionTime = Find<Text>(instance, "MissionTime");
        _mode = Find<Text>(instance, "Mode");
        _rtaStatus = Find<Text>(instance, "RtaStatus");
        _runTime = Find<Text>(instance, "RunTime");
        _zoom = Find<Text>(instance, "Zoom");
        _packetLoss = Find<Graphic>(instance, "PacketLoss");

        if (_packetLoss) { _packetLoss!.enabled = false; }

        _nextPacketLossBurst = Time.unscaledTime + UnityEngine.Random.Range(60f, 180f);

        Instance = this;

        CagLog.Info($"[WTT-CAG] Built HUD '{prefab.name}' | {_texture!.width}x{_texture.height} | display: {(bool)_display} tape: {(bool)_compassTape} heading: {(bool)_heading} missionTime: {(bool)_missionTime} mode: {(bool)_mode} rta: {(bool)_rtaStatus} runTime: {(bool)_runTime} zoom: {(bool)_zoom} packetLoss: {(bool)_packetLoss}");
    }

    private static T? Find<T>(GameObject root, string elementName) where T : Component
    {
        foreach (var candidate in root.GetComponentsInChildren<T>(true))
        {
            if (candidate.gameObject.name == elementName) { return candidate; }
        }

        return null;
    }

    private void AllocateTexture()
    {
        var width = Mathf.Max(Screen.width, 16);
        var height = Mathf.Max(Screen.height, 16);

        if (_texture && _texture!.width == width && _texture.height == height) { return; }

        ReleaseTexture();

        _texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            name = "CAG_ThermalHudRT",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false
        };

        _texture.Create();

        var previous = RenderTexture.active;
        RenderTexture.active = _texture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = previous;

        _hudCamera!.targetTexture = _texture;
        _dirty = true;
    }

    private void ReleaseTexture()
    {
        if (!_texture) { return; }

        if (_hudCamera && _hudCamera!.targetTexture == _texture)
        {
            _hudCamera.targetTexture = null;
        }

        _texture!.Release();
        Destroy(_texture);
        _texture = null;
    }

    public bool TryGetDisplayRect(out Vector4 rect)
    {
        rect = default;

        if (!_display || !_hudCamera) { return false; }

        _display!.GetWorldCorners(_corners);

        var min = _hudCamera!.WorldToViewportPoint(_corners[0]);
        var max = _hudCamera.WorldToViewportPoint(_corners[2]);

        rect = new Vector4(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));

        return rect.z > rect.x && rect.w > rect.y;
    }

    public void SetVisible(bool visible)
    {
        if (_visible == visible) { return; }

        _visible = visible;
        _dirty = visible;

        if (!visible && _hudCamera) { _hudCamera!.enabled = false; }
    }

    public bool IsVisible => _visible;

    private void Update()
    {
        if (!_hudCamera || !_visible || !_hostCamera) { return; }

        AllocateTexture();

        ApplyPlacement();
        UpdateCompass();
        UpdatePacketLoss();

        if (Time.unscaledTime >= _nextSlowUpdate) { SlowUpdate(); }

        // Render this frame only if something drawn on the HUD changed.
        _hudCamera!.enabled = _dirty;
        _dirty = false;
    }

    private void SlowUpdate()
    {
        _nextSlowUpdate = Time.unscaledTime + SlowUpdateInterval;

        SetText(_missionTime, FormatClock(RaidClock.Remaining));

        if (Profile != null)
        {
            SetText(_runTime, FormatClock(TimeSpan.FromSeconds(FusionThermalState.RunTimeSeconds(Profile))));
            SetText(_mode, FusionThermalState.ModeLabel(Profile));
            SetText(_zoom, FusionThermalState.ZoomLabel(Profile));
        }

        if (Time.unscaledTime >= _nextRtaScan)
        {
            _nextRtaScan = Time.unscaledTime + RtaScanInterval;
            _rtaLinked = IsCarryingRtaSight();
        }

        SetText(_rtaStatus, _rtaLinked ? "RTA LINKED" : "RTA NO LINK");
    }

    private void UpdateCompass()
    {
        // Each map defines its own north as a yaw angle in LevelSettings.
        var levelSettings = Singleton<LevelSettings>.Instance;
        var mapNorth = levelSettings ? levelSettings.NorthDirection : 0f;

        var heading = Mathf.Repeat(_hostCamera!.transform.eulerAngles.y - mapNorth + ThermalConfigSection.CompassNorthOffset.Value, 360f);

        if (_compassTape && (float.IsNaN(_lastTapeHeading) || Mathf.Abs(heading - _lastTapeHeading) > 0.02f))
        {
            _lastTapeHeading = heading;

            var window = _compassTape!.uvRect;
            window.x = Mathf.Repeat(heading - TapeStartHeading, 360f) / 360f - window.width * 0.5f;
            _compassTape.uvRect = window;
            _dirty = true;
        }

        var degrees = Mathf.RoundToInt(heading) % 360;

        if (degrees == _lastHeadingDegrees) { return; }

        _lastHeadingDegrees = degrees;
        SetText(_heading, $"{degrees}\u00b0 M");
    }

    private void ApplyPlacement()
    {
        if (!_display || Profile == null) { return; }

        var offset = new Vector2(Profile.HudOffsetX.Value, Profile.HudOffsetY.Value);
        var scale = Mathf.Max(Profile.HudScale.Value, 0.01f);

        if (Mathf.Abs(offset.x - _appliedOffset.x) < 0.01f && Mathf.Abs(offset.y - _appliedOffset.y) < 0.01f && Mathf.Abs(scale - _appliedScale) < 0.0001f)
        {
            return;
        }

        _appliedOffset = offset;
        _appliedScale = scale;

        _display!.anchoredPosition = _authoredPosition + offset;
        _display.localScale = new Vector3(scale, scale, 1f);
        _dirty = true;
    }

    private void UpdatePacketLoss()
    {
        if (!_packetLoss) { return; }

        var now = Time.unscaledTime;

        if (_packetLossBlinksLeft == 0)
        {
            if (now < _nextPacketLossBurst) { return; }

            _packetLossBlinksLeft = UnityEngine.Random.Range(3, 7) * 2;
            _nextPacketLossBlink = now;
        }

        if (now < _nextPacketLossBlink) { return; }

        _packetLossBlinksLeft--;
        _packetLoss!.enabled = _packetLossBlinksLeft % 2 == 1;
        _dirty = true;
        _nextPacketLossBlink = now + 0.18f;

        if (_packetLossBlinksLeft == 0)
        {
            _nextPacketLossBurst = now + UnityEngine.Random.Range(60f, 180f);
        }
    }

    // The RTA link pairs the goggle with a thermal sight on the weapon in the player's hands.
    private static bool IsCarryingRtaSight()
    {
        if (Singleton<GameWorld>.Instance?.MainPlayer?.HandsController?.Item is not Weapon weapon) { return false; }

        // The same call the game's WeaponPrefab uses to list a weapon and all its attachments.
        WeaponParts.Clear();
        weapon.GetAllItemsNonAlloc(WeaponParts, false, true);

        foreach (var item in WeaponParts)
        {
            if (item != null && item.StringTemplateId == HolosunThermalOptic.TemplateId) { return true; }
        }

        return false;
    }



    private static string FormatClock(TimeSpan? time)
    {
        if (time == null) { return BlankClock; }

        var value = time.Value;

        return $"{(int)value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00}";
    }

    private void SetText(Text? element, string value)
    {
        if (!element || element!.text == value) { return; }

        element.text = value;
        _dirty = true;
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; }

        ReleaseTexture();

        CagLog.Info($"[WTT-CAG] Destroyed {name}");
    }
}