using System;
using Comfort.Common;
using EFT;
using UnityEngine;
using UnityEngine.UI;
using WTT_CAG.Client.Config;
using WTT_CAG.Client.Content.Thermals.Holosun;
using WTT_CAG.Client.Core.Thermal;

namespace WTT_CAG.Client.Content.Thermals.Fusion;

// The goggle's heads-up display. The HUD itself is a prefab authored in Unity; this instantiates
// it, renders it into a texture with a camera that sees nothing else, and keeps its live elements
// up to date. The texture is handed to the fusion shader as its device overlay, so the HUD is
// composited with everything else after the upscale.
public class FusionThermalHud : MonoBehaviour
{
    private const string CameraObjectName = "WTT_CAG_FusionThermalHudCamera";

    // Unity's built-in UI layer. The HUD camera renders this layer and nothing else.
    private const int HudLayer = 5;

    // Far below any map, so no gameplay camera ever has the HUD canvas in view and the HUD
    // camera never has anything but its own canvas in view.
    private static readonly Vector3 ParkedPosition = new(0f, -5000f, 0f);

    // Must render before the zone camera and the FPS camera so the texture is ready for the composite.
    private const float DepthOffset = -20f;

    // The compass tape texture: which heading sits at its left edge.
    private const float TapeStartHeading = 26f;

    // Clocks, mode and link status only need checking a few times a second.
    private const float SlowUpdateInterval = 0.25f;
    private const float RtaScanInterval = 1f;

    private const string BlankClock = "--:--:--";

    public ThermalDeviceProfile? Profile { get; private set; }

    public static FusionThermalHud? Instance { get; private set; }

    private Camera? _hostCamera;
    private Camera? _hudCamera;
    private RenderTexture? _texture;

    // The HUD's canvas is 1080 units tall whatever the screen size (Canvas Scaler, match height).
    public const float CanvasHeight = 1080f;

    private RectTransform? _display;
    private readonly Vector3[] _corners = new Vector3[4];

    private RawImage? _compassTape;
    private Text? _heading;
    private Text? _missionTime;
    private Text? _mode;
    private Text? _rtaStatus;
    private Text? _runTime;
    private Graphic? _packetLoss;

    private float _lastTapeHeading = float.NaN;
    private int _lastHeadingDegrees = -1;
    private float _nextSlowUpdate;
    private float _nextRtaScan;
    private bool _rtaLinked;

    private float _nextPacketLossBurst;
    private int _packetLossBlinksLeft;
    private float _nextPacketLossBlink;

    // The texture the shader samples, or null when there is no HUD to draw.
    public Texture? Texture => _texture;

    // Returns null when the profile has no HUD or its prefab cannot be loaded. The goggle works without one.
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

        // Transparent white rather than transparent black: the UI blends into this, and a white
        // base keeps the soft edges of the lettering from darkening.
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

        // Every element is optional, so a simpler goggle can leave out what it does not have.
        _display = Find<RectTransform>(instance, "Display");
        _compassTape = Find<RawImage>(instance, "CompassTape");
        _heading = Find<Text>(instance, "Heading");
        _missionTime = Find<Text>(instance, "MissionTime");
        _mode = Find<Text>(instance, "Mode");
        _rtaStatus = Find<Text>(instance, "RtaStatus");
        _runTime = Find<Text>(instance, "RunTime");
        _packetLoss = Find<Graphic>(instance, "PacketLoss");

        if (_packetLoss) { _packetLoss!.enabled = false; }

        _nextPacketLossBurst = Time.unscaledTime + UnityEngine.Random.Range(60f, 180f);

        Instance = this;

        Plugin.logger!.LogInfo($"[WTT-CAG] Built HUD '{prefab.name}' | {_texture!.width}x{_texture.height} | display: {(bool)_display} tape: {(bool)_compassTape} heading: {(bool)_heading} missionTime: {(bool)_missionTime} mode: {(bool)_mode} rta: {(bool)_rtaStatus} runTime: {(bool)_runTime} packetLoss: {(bool)_packetLoss}");
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

        // Start transparent, so nothing stray is composited before the HUD camera's first render.
        var previous = RenderTexture.active;
        RenderTexture.active = _texture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = previous;

        _hudCamera!.targetTexture = _texture;
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

    // The Display panel's area on screen, as xy = bottom-left and zw = top-right in screen UV - the
    // same space the fusion shader works in, because the HUD texture maps 1:1 onto the screen.
    public bool TryGetDisplayRect(out Vector4 rect)
    {
        rect = default;

        if (!_display || !_hudCamera) { return false; }

        _display!.GetWorldCorners(_corners);

        // Corners run bottom-left, top-left, top-right, bottom-right.
        var min = _hudCamera!.WorldToViewportPoint(_corners[0]);
        var max = _hudCamera.WorldToViewportPoint(_corners[2]);

        rect = new Vector4(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));

        return rect.z > rect.x && rect.w > rect.y;
    }

    // Stops the HUD camera rendering while the tube is off.
    public void SetVisible(bool visible)
    {
        if (_hudCamera && _hudCamera!.enabled != visible) { _hudCamera.enabled = visible; }
    }

    private void Update()
    {
        if (!_hudCamera || !_hudCamera!.enabled || !_hostCamera) { return; }

        // The texture matches the screen, so it follows a resolution change.
        AllocateTexture();

        UpdateCompass();
        UpdatePacketLoss();

        if (Time.unscaledTime < _nextSlowUpdate) { return; }

        _nextSlowUpdate = Time.unscaledTime + SlowUpdateInterval;

        SetText(_missionTime, FormatClock(RaidClock.Remaining));
        SetText(_runTime, FormatClock(TimeSpan.FromSeconds(FusionThermalState.RunTimeSeconds)));

        if (Profile != null) { SetText(_mode, FusionThermalState.ModeLabel(Profile)); }

        if (Time.unscaledTime >= _nextRtaScan)
        {
            _nextRtaScan = Time.unscaledTime + RtaScanInterval;
            _rtaLinked = IsCarryingRtaSight();
        }

        SetText(_rtaStatus, _rtaLinked ? "RTA LINKED" : "RTA NO LINK");
    }

    private void UpdateCompass()
    {
        // The direction the head camera faces, clockwise from the map's +Z axis, plus the tunable offset to north.
        var heading = Mathf.Repeat(_hostCamera!.transform.eulerAngles.y + ThermalConfigSection.CompassNorthOffset.Value, 360f);

        // The IsNaN check covers the first frame: any comparison with NaN is false, so without it
        // the tape would never be positioned at all.
        if (_compassTape && (float.IsNaN(_lastTapeHeading) || Mathf.Abs(heading - _lastTapeHeading) > 0.02f))
        {
            _lastTapeHeading = heading;

            // Slide the visible window so the current heading sits in its centre. The texture repeats, so any value is valid.
            var window = _compassTape!.uvRect;
            window.x = Mathf.Repeat(heading - TapeStartHeading, 360f) / 360f - window.width * 0.5f;
            _compassTape.uvRect = window;
        }

        var degrees = Mathf.RoundToInt(heading) % 360;

        if (degrees == _lastHeadingDegrees) { return; }

        _lastHeadingDegrees = degrees;
        SetText(_heading, $"{degrees}\u00b0 M");
    }

    // Mostly off, with a short run of blinks at random intervals.
    private void UpdatePacketLoss()
    {
        if (!_packetLoss) { return; }

        var now = Time.unscaledTime;

        if (_packetLossBlinksLeft == 0)
        {
            if (now < _nextPacketLossBurst) { return; }

            // Each blink is one switch on and one switch off.
            _packetLossBlinksLeft = UnityEngine.Random.Range(3, 7) * 2;
            _nextPacketLossBlink = now;
        }

        if (now < _nextPacketLossBlink) { return; }

        _packetLossBlinksLeft--;
        _packetLoss!.enabled = _packetLossBlinksLeft % 2 == 1;
        _nextPacketLossBlink = now + 0.18f;

        if (_packetLossBlinksLeft == 0)
        {
            _nextPacketLossBurst = now + UnityEngine.Random.Range(60f, 180f);
        }
    }

    // The RTA link pairs the goggle with a thermal weapon sight, so it shows as linked while the
    // player has one on them.
    private static bool IsCarryingRtaSight()
    {
        var items = Singleton<GameWorld>.Instance?.MainPlayer?.Profile?.Inventory?.AllRealPlayerItems;

        if (items == null) { return false; }

        foreach (var item in items)
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

    // Only touches the text when it changes, so the canvas is not rebuilt for nothing.
    private static void SetText(Text? element, string value)
    {
        if (element && element!.text != value) { element.text = value; }
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; }

        ReleaseTexture();

        Plugin.logger!.LogInfo($"[WTT-CAG] Destroyed {name}");
    }
}
