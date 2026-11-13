using UnityEngine;

namespace WTT_CAG.Client.Content.Thermals.Fusion;

// The post-upscale composite, used by composite mode 2. Everything reachable on the FPS camera
// happens before DLSS/FSR, so anything drawn there is clipped to the pre-upscale region. This is a
// second camera that draws no geometry and renders after the FPS camera has finished, which gives
// an OnRenderImage at full output resolution where UV maps 1:1 to the screen.
public class FusionThermalOverlayCamera : MonoBehaviour
{
    private const string CameraObjectName = "WTT_CAG_FusionThermalOverlayCamera";

    // High enough to render after the FPS camera's upscale, low enough to stay under the UI
    // cameras. At +100 the overlay drew on top of the pause menu.
    private const float DepthOffset = 1f;

    public Material? OverlayMaterial;
    public bool Active = true;

    private Camera? _hostCamera;
    private Camera? _overlayCamera;

    public static FusionThermalOverlayCamera? Create(Camera hostCamera, Material material)
    {
        if (!hostCamera)
        {
            Plugin.logger!.LogError("[WTT-CAG] Host camera is null.");
            return null;
        }

        var go = new GameObject(CameraObjectName);

        go.SetActive(false);
        go.transform.SetParent(hostCamera.transform, false);

        var self = go.AddComponent<FusionThermalOverlayCamera>();
        self._hostCamera = hostCamera;
        self.OverlayMaterial = material;
        self.Build();

        return self;
    }

    private void Build()
    {
        // Deliberately not CopyFrom: the host's culling mask, effects and SSAA state must not come across.
        _overlayCamera = gameObject.AddComponent<Camera>();

        _overlayCamera.depth = _hostCamera!.depth + DepthOffset;
        _overlayCamera.clearFlags = CameraClearFlags.Nothing;
        _overlayCamera.cullingMask = 0;
        _overlayCamera.useOcclusionCulling = false;
        _overlayCamera.allowHDR = false;
        _overlayCamera.allowMSAA = false;
        _overlayCamera.renderingPath = RenderingPath.Forward;
        _overlayCamera.targetTexture = null;

        gameObject.SetActive(true);

        Plugin.logger!.LogInfo($"[WTT-CAG] Built overlay camera | depth: {_overlayCamera.depth} (host: {_hostCamera.depth}) | {Screen.width}x{Screen.height}");
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination);

        // Menus keep rendering the world behind them. timeScale is the reliable signal that the
        // game is paused, so the overlay stays off the pause screen.
        if (!Active || !OverlayMaterial || Time.timeScale <= 0.0001f) { return; }

        // The material alpha-blends and is transparent outside the zones.
        Graphics.Blit(source, destination, OverlayMaterial);
    }

    public void SetCameraEnabled(bool on)
    {
        if (!_overlayCamera || _overlayCamera!.enabled == on) { return; }

        _overlayCamera.enabled = on;
    }

    private void OnDestroy()
    {
        Plugin.logger!.LogInfo($"[WTT-CAG] Destroyed {name}");
    }
}
