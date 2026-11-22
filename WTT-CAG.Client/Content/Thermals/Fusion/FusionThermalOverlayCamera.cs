using UnityEngine;
using UnityEngine.Rendering;
using WTT_CAG.Client.Core;

namespace WTT_CAG.Client.Content.Thermals.Fusion;

// Draws the composite after the FPS camera's upscale, at full output resolution. Uses a command buffer
// rather than OnRenderImage so the frame is never copied.
public class FusionThermalOverlayCamera : MonoBehaviour
{
    private const string CameraObjectName = "WTT_CAG_FusionThermalOverlayCamera";

    // Above the FPS camera, below the UI cameras.
    private const float DepthOffset = 1f;

    public Material? OverlayMaterial;
    public bool Active = true;

    private Camera? _hostCamera;
    private Camera? _overlayCamera;
    private CommandBuffer? _commandBuffer;
    private bool _drawing;

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
        _overlayCamera = gameObject.AddComponent<Camera>();

        _overlayCamera.depth = _hostCamera!.depth + DepthOffset;
        _overlayCamera.clearFlags = CameraClearFlags.Nothing;
        _overlayCamera.cullingMask = 0;
        _overlayCamera.useOcclusionCulling = false;
        _overlayCamera.allowHDR = false;
        _overlayCamera.allowMSAA = false;
        _overlayCamera.renderingPath = RenderingPath.Forward;
        _overlayCamera.targetTexture = null;

        _commandBuffer = new CommandBuffer { name = "Fusion Thermal Overlay" };
        _overlayCamera.AddCommandBuffer(CameraEvent.AfterEverything, _commandBuffer);

        gameObject.SetActive(true);

        CagLog.Info($"[WTT-CAG] Built overlay camera | depth: {_overlayCamera.depth} (host: {_hostCamera.depth}) | {Screen.width}x{Screen.height}");
    }

    private void LateUpdate()
    {
        if (_commandBuffer == null) { return; }

        // timeScale is zero while paused, which keeps the overlay off the pause screen.
        var draw = Active && OverlayMaterial && Time.timeScale > 0.0001f;

        if (draw == _drawing) { return; }

        _drawing = draw;
        _commandBuffer.Clear();

        if (!draw) { return; }

        _commandBuffer.Blit(Texture2D.blackTexture, BuiltinRenderTextureType.CameraTarget, OverlayMaterial);
    }

    public void SetCameraEnabled(bool on)
    {
        if (!_overlayCamera || _overlayCamera!.enabled == on) { return; }

        _overlayCamera.enabled = on;
    }

    private void OnDestroy()
    {
        if (_overlayCamera && _commandBuffer != null)
        {
            _overlayCamera!.RemoveCommandBuffer(CameraEvent.AfterEverything, _commandBuffer);
        }

        _commandBuffer?.Release();
        _commandBuffer = null;

        CagLog.Info($"[WTT-CAG] Destroyed {name}");
    }
}
