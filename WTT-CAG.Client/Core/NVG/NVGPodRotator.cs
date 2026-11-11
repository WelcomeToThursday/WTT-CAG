using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BSG.CameraEffects;
using EFT.CameraControl;
using UnityEngine;
using WTT_CAG.Client.Compat;
using WTT_CAG.Client.Config;

namespace WTT_CAG.Client.Core;

public class NVGPodRotator : MonoBehaviour
{
    private CurveRotator _baseRotator;
    private NVGDeviceProfile _profile;
    private FieldInfo _float0Field;
    private Transform _leftPod;
    private Transform _rightPod;

    private float _manualTLeft = 1f;
    private float _manualTargetTLeft = 1f;
    private float _manualTRight = 1f;
    private float _manualTargetTRight = 1f;
    private bool _leftOverride;
    private bool _rightOverride;

    private bool _wasFullyStowed;
    private bool _wasLeftStowed;
    private bool _wasRightStowed;

    public static Texture ActiveMask = null;
    public static NightVision ManagedInstance = null;

    public enum PodSide
    {
        None,
        Left,
        Right,
        Both
    }
    public static PodSide StowedPod = PodSide.None;

    public static NVGDeviceProfile ActiveProfile;

    private NightVision _nightVision;

    private Texture _originalAnvis;
    private Texture _originalBino;
    private Texture _originalMono;

    private Texture2D _maskFull;
    private Texture2D _maskLeft;
    private Texture2D _maskRight;
    private bool _masksBuilt;

    private static readonly FieldInfo? _material0Field = FindField(typeof(TextureMask), typeof(Material), "material_0");

    private static readonly MethodInfo? _tryToEnable = typeof(TextureMask).GetMethod("TryToEnable", BindingFlags.Public | BindingFlags.Instance);


    public void Init(CurveRotator rotator, NVGDeviceProfile nvgProfile)
    {
        _baseRotator = rotator;
        _profile = nvgProfile;

        _float0Field = FindField(typeof(CurveRotator), typeof(float), "float_0", "_t");

        var searchRoot = string.IsNullOrEmpty(_profile.ChildNVGName) ? transform : FindChildContainingStatic(transform, _profile.ChildNVGName);

        if (!searchRoot)
        {
            return;
        }

        _leftPod = searchRoot!.Find(_profile.LeftPodPath);
        _rightPod = searchRoot.Find(_profile.RightPodPath);
    }
        
    private bool IsLocalPlayerNVG() => transform.root && transform.root.name.StartsWith("PlayerSuperior");

    private void EnsureNightVisionCached()
    {
        if (_nightVision)
        {
            return;
        }

        _nightVision = CameraManager.Instance?.NightVision!;

        if (!_nightVision || _originalAnvis)
        {
            return;
        }
            
        _originalAnvis = _nightVision!.AnvisMaskTexture;
        _originalBino = _nightVision.BinocularMaskTexture;
        _originalMono = _nightVision.OldMonocularMaskTexture;
            
        Plugin.logger!.LogDebug("[NvgPodRotator] NightVision cached via CameraManager.Instance");
            
        StartCoroutine(LogShaderDiagnostics());
    }

    private IEnumerator LogShaderDiagnostics()
    {
        yield return new WaitForSeconds(5f);

        if (_nightVision?.TextureMask != null)
        {
            // Reflection by name - not a build error if it changes, so guard it
            var mat = _material0Field != null
                ? (Material)_material0Field.GetValue(_nightVision.TextureMask)
                : null;
            if (_material0Field == null)
                Plugin.logger!.LogDebug("[NvgPodRotator] TextureMask.material_0 not found - field name may have changed in 4.1");
            Plugin.logger!.LogDebug($"[NvgPodRotator] TextureMask.Shader: {_nightVision.TextureMask.Shader?.name ?? "NULL"}");
            Plugin.logger!.LogDebug($"[NvgPodRotator] TextureMask material shader: {mat?.shader?.name ?? "NULL"}");
            Plugin.logger!.LogDebug($"[NvgPodRotator] TextureMask GameObject: {_nightVision.TextureMask.gameObject.name}");
            Plugin.logger!.LogDebug($"[NvgPodRotator] Same GameObject as NightVision: {_nightVision.TextureMask.gameObject == _nightVision.gameObject}");
            Plugin.logger!.LogDebug($"[NvgPodRotator] TextureMask enabled: {_nightVision.TextureMask.enabled}");
            Plugin.logger!.LogDebug($"[NvgPodRotator] Mask on material: {mat?.GetTexture("_Mask")?.name ?? "NULL"}");
        }
        else
        {
            Plugin.logger!.LogDebug("[NvgPodRotator] TextureMask is null after 5s");
        }
    }


    public void OnMountStateChanged(bool isOn, bool initial)
    {
        if (!IsLocalPlayerNVG()) return;

        EnsureNightVisionCached();

        if (isOn && (_leftOverride || _rightOverride))
        {
            _leftOverride = false;
            _rightOverride = false;
        }
            
        if (BorkelsNVGCompat.Active)
        {
            return;
        }

        if (!isOn || !_nightVision)
        {
            return;
        }
        EnsureMasksBuilt();

        if (_maskFull)
        {
            ApplyCustomMask(_maskFull);
        }
        else RestoreOriginalMask();
    }

    private void Update()
    {
        if (!_baseRotator || !_leftPod || !_rightPod || _float0Field == null)
        {
                
        }

        var mountProgress = (float)_float0Field.GetValue(_baseRotator);
        var mountT = _baseRotator.AnimationCurve.Evaluate(mountProgress);
        var mountDeployed = mountProgress > 0.05f;

        var isLocal = IsLocalPlayerNVG();
        if (isLocal) ActiveProfile = _profile;

        if (isLocal && mountDeployed)
        {
            if (KeyboardConfigSection.BothPodsFoldKey!.Value.IsDown())
            {
                TogglePod(ref _leftOverride, ref _manualTargetTLeft, ref _manualTLeft);
                TogglePod(ref _rightOverride, ref _manualTargetTRight, ref _manualTRight);
                PlayPodFlipFeedback();
            }
            else if (KeyboardConfigSection.LeftPodFoldKey!.Value.IsDown())
            {
                TogglePod(ref _leftOverride, ref _manualTargetTLeft, ref _manualTLeft);
                PlayPodFlipFeedback();
            }
            else if (KeyboardConfigSection.RightPodFoldKey!.Value.IsDown())
            {
                TogglePod(ref _rightOverride, ref _manualTargetTRight, ref _manualTRight);
                PlayPodFlipFeedback();
            }
        }

        var tLeft = Animate(ref _manualTLeft, ref _manualTargetTLeft, _leftOverride, mountT);
        var tRight = Animate(ref _manualTRight, ref _manualTargetTRight, _rightOverride, mountT);

        _leftPod.localRotation = Quaternion.Lerp(_profile.LeftOff, _profile.LeftOn, tLeft);
        _rightPod.localRotation = Quaternion.Lerp(_profile.RightOff, _profile.RightOn, tRight);

        if (!isLocal) return;

        var targetLeft = _leftOverride ? _manualTargetTLeft : mountT;
        var targetRight = _rightOverride ? _manualTargetTRight : mountT;

        var leftStowed = targetLeft < 0.5f;
        var rightStowed = targetRight < 0.5f;
        var bothStowed = leftStowed && rightStowed;

        StowedPod = bothStowed ? PodSide.Both : leftStowed ? PodSide.Left : rightStowed ? PodSide.Right : PodSide.None;

        switch (bothStowed)
        {
            case true when !_wasFullyStowed:
            {
                OnBothPodsStowed();
                break;
            }
            case false when _wasFullyStowed:
            {
                OnPodsDeployed();
                break;
            }
            case false:
                switch (leftStowed)
                {
                    case true when !_wasLeftStowed:
                    {
                        OnSinglePodStowed(leftStowed: true);
                        break;
                    }
                    case false when _wasLeftStowed:
                    {
                        OnSinglePodDeployed();
                        break;
                    }
                }

                switch (rightStowed)
                {
                    case true when !_wasRightStowed:
                    {
                        OnSinglePodStowed(leftStowed: false);
                        break;
                    }
                    case false when _wasRightStowed:
                    {
                        OnSinglePodDeployed();
                        break;
                    }
                }

                break;
        }

        _wasFullyStowed = bothStowed;
        _wasLeftStowed = leftStowed;
        _wasRightStowed = rightStowed;
    }

    private void LateUpdate()
    {
        if (!IsLocalPlayerNVG() || !_nightVision || !_baseRotator || _float0Field == null)
        {
            return;
        }

        var mountProgress = (float)_float0Field.GetValue(_baseRotator);
        var mountDeployed = mountProgress > 0.05f;
            
        if (BorkelsNVGCompat.Active)
        {
            switch (_wasFullyStowed)
            {
                case true when _nightVision.On:
                {
                    _nightVision.On = false;
                    _nightVision.ApplySettings();
                    break;
                }
                case false when mountDeployed && !_nightVision.On:
                {
                    _nightVision.On = true;
                    _nightVision.ApplySettings();
                    break;
                }
            }

            return;
        }

        if (_wasFullyStowed)
        {
            _nightVision.On = false;
            _tryToEnable?.Invoke(_nightVision.TextureMask, [
                _nightVision, false
            ]);
        }
        else if (mountDeployed && !_nightVision.On)
        {
            _nightVision.On = true;
            _tryToEnable?.Invoke(_nightVision.TextureMask, [
                _nightVision, true
            ]);

            EnsureMasksBuilt();

            if (_maskFull)
            {
                ApplyCustomMask(_maskFull);
            }
            else RestoreOriginalMask();
        }
    }

    private void OnDestroy()
    {
        if (ActiveProfile != _profile)
        {
            return;
        }
        ActiveProfile = null;
        StowedPod = PodSide.None;
    }

    private static FieldInfo FindField(Type owner, Type fieldType, string oldName, params string[] knownNames)
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        var byName = owner.GetField(oldName, flags);
        if (byName != null && byName.FieldType == fieldType) return byName;

        foreach (var n in knownNames)
        {
            var f = owner.GetField(n, flags);

            if (f != null && f.FieldType == fieldType)
            {
                return f;
            }
        }

        var candidates = new List<FieldInfo>();
        foreach (var f in owner.GetFields(flags))
        {
            if (f.FieldType == fieldType)
            {
                candidates.Add(f);
            }
        }

        if (candidates.Count == 1)
        {
            Plugin.logger!.LogDebug($"[NvgPodRotator] {owner.Name}.{oldName} is now '{candidates[0].Name}'");
            return candidates[0];
        }

        Plugin.logger!.LogWarning($"[WTT-CAG] {owner.Name}: {candidates.Count} private {fieldType.Name} fields, can't tell which replaced {oldName}. All private fields:");
        foreach (var f in owner.GetFields(flags))
        {
            Plugin.logger!.LogWarning($"{f.FieldType.Name} {f.Name}");
        }
        return null;
    }


    private void OnBothPodsStowed()
    {
        if (BorkelsNVGCompat.Active)
        {
            RefreshBorkel();
            return;
        }

        ActiveMask = null;
        Plugin.logger!.LogDebug("[NvgPodRotator] Both pods stowed → NVG OFF");
    }

    private void OnPodsDeployed()
    {
        if (BorkelsNVGCompat.Active)
        {
            RefreshBorkel();
            return;
        }

        EnsureNightVisionCached();

        if (!_nightVision)
        {
            return;
        }

        EnsureMasksBuilt();

        if (_maskFull)
        {
            ApplyCustomMask(_maskFull);
        }
        else RestoreOriginalMask();
    }

    private void OnSinglePodStowed(bool leftStowed)
    {
        if (BorkelsNVGCompat.Active)
        {
            RefreshBorkel();
            return;
        }

        EnsureNightVisionCached();

        if (!_nightVision || _profile.SinglePodMask == null)
        {
            return;
        }

        EnsureMasksBuilt();
        var shiftedTex = leftStowed ? _maskRight : _maskLeft;

        if (shiftedTex)
        {
            ApplyCustomMask(shiftedTex);
        }
        else RestoreOriginalMask();
    }

    private void OnSinglePodDeployed()
    {
        if (BorkelsNVGCompat.Active)
        {
            RefreshBorkel();
            return;
        }

        EnsureNightVisionCached();

        if (!_nightVision)
        {
            return;
        }

        EnsureMasksBuilt();

        if (_maskFull)
        {
            ApplyCustomMask(_maskFull);
        }
        else RestoreOriginalMask();
    }
        
    private void RefreshBorkel()
    {
        EnsureNightVisionCached();

        if (_nightVision && _nightVision.On)
        {
            _nightVision.ApplySettings();
        }
    }

    private void ApplyCustomMask(Texture mask)
    {
        if (BorkelsNVGCompat.Active)
        {
            return;
        }

        if (!_nightVision || !mask)
        {
            return;
        }

        NVGPodRotator.ManagedInstance = _nightVision;
        NVGPodRotator.ActiveMask = mask;
        _nightVision.ApplySettings();

        if (_nightVision.TextureMask && !_nightVision.TextureMask.enabled)
        {
            _tryToEnable?.Invoke(_nightVision.TextureMask, [
                _nightVision, true
            ]);
        }

        Plugin.logger!.LogDebug($"[NvgPodRotator] ApplyCustomMask: '{mask.name}'");
    }

    private void RestoreOriginalMask()
    {
        NVGPodRotator.ActiveMask = null;
        NVGPodRotator.ManagedInstance = null;

        if (BorkelsNVGCompat.Active) return;
        if (!_nightVision) return;
        if (_originalAnvis) _nightVision.AnvisMaskTexture = _originalAnvis;
        if (_originalBino) _nightVision.BinocularMaskTexture = _originalBino;
        if (_originalMono) _nightVision.OldMonocularMaskTexture = _originalMono;

        if (_profile.SinglePodMask.HasValue)
            _nightVision.SetMask(_profile.SinglePodMask.Value);

        Plugin.logger!.LogDebug("[NvgPodRotator] Restored default game mask logic");
    }
        
    private void EnsureMasksBuilt()
    {
        if (_masksBuilt) return;

        var masksFolder = Path.Combine(Plugin.PluginFolder, "masks");

        switch (_profile.PrefabNameContains)
        {
            case "Chimeara":
            {
                _maskFull = TryLoadPng(Path.Combine(masksFolder, "chimera_full.png"), "chimera_full") ?? TryLoadPng(Path.Combine(masksFolder, "mask_chimera_full.png"), "chimera_full");
                _maskLeft = TryLoadPng(Path.Combine(masksFolder, "chimera_left.png"), "chimera_left") ?? TryLoadPng(Path.Combine(masksFolder, "mask_chimera_left.png"), "chimera_left");
                _maskRight = TryLoadPng(Path.Combine(masksFolder, "chimera_right.png"), "chimera_right") ?? TryLoadPng(Path.Combine(masksFolder, "mask_chimera_right.png"), "chimera_right");
                break;
            }
            case "nvg_wilcox_l4g24_mount":
            {
                _maskFull = TryLoadPng(Path.Combine(masksFolder, "mask_binocular.png"), "mask_binocular");
                _maskLeft = TryLoadPng(Path.Combine(masksFolder, "mask_old_monocular.png"), "mask_old_monocular");
                _maskRight = TryLoadPng(Path.Combine(masksFolder, "mask_old_monocular.png"), "mask_old_monocular");
                break;
            }
            default:
            {
                _maskLeft = TryLoadPng(Path.Combine(masksFolder, "mask_left.png"), "mask_left");
                _maskRight = TryLoadPng(Path.Combine(masksFolder, "mask_right.png"), "mask_right");
                break;
            }
        }

        if (!_maskLeft || !_maskRight)
        {
            var sourceMask = _maskFull ? _maskFull : _originalAnvis;

            if (sourceMask)
            {
                var readable = MakeReadableCopy(sourceMask!);

                if (readable)
                {
                    var shift = readable!.width / 4;

                    if (!_maskLeft)
                    {
                        _maskLeft = GenerateShiftedMask(readable, -shift, "mask_left_gen");
                    }

                    if (!_maskRight)
                    {
                        _maskRight = GenerateShiftedMask(readable, shift, "mask_right_gen");
                    }
                        
                    Destroy(readable);
                }
            }
        }

        _masksBuilt = true;
        Plugin.logger!.LogDebug($"[NvgPodRotator] Masks built — full:{_maskFull?.name ?? "NULL"} left:{_maskLeft?.name ?? "NULL"} right:{_maskRight?.name ?? "NULL"}");
    }

    private static Texture2D? TryLoadPng(string path, string texName)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (!tex.LoadImage(File.ReadAllBytes(path)))
            {
                return null;
            }
            tex.name = texName;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }
        catch (Exception)
        {
            return null;
        }
    }

    internal static Texture2D? MakeReadableCopy(Texture source)
    {
        try
        {
            var rt = RenderTexture.GetTemporary(source.width, source.height, 0);
            var prev = RenderTexture.active;
            Graphics.Blit(source, rt);
            RenderTexture.active = rt;
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply();
            copy.wrapMode = TextureWrapMode.Clamp;
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }
        catch (Exception)
        {
            return null;
        }
    }

    internal static Texture2D GenerateShiftedMask(Texture2D src, int shiftX, string texName)
    {
        var w = src.width;
        var h = src.height;
        var srcP = src.GetPixels();
        var dstP = new Color[w * h];

        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var sx = x - shiftX;
            dstP[y * w + x] = sx >= 0 && sx < w ? srcP[y * w + sx] : Color.black;
        }

        var result = new Texture2D(w, h, TextureFormat.RGBA32, false);
        result.name = texName;
        result.wrapMode = TextureWrapMode.Clamp;
        result.SetPixels(dstP);
        result.Apply();
        return result;
    }
        
    private static readonly MethodInfo? _headReachAnim = typeof(EFT.Player).GetMethod("SwitchHeadLightsAnimation", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
    private static readonly MethodInfo? _nvgClickSound = typeof(EFT.Player).GetMethod("PlayNightVisionSound", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);

    private static void PlayPodFlipFeedback()
    {
        var player = Comfort.Common.Singleton<EFT.GameWorld>.Instance?.MainPlayer;
        if (!player) { return; }

        try
        {
            if (NVGConfigSection.PodFlipAnimation.Value) _headReachAnim?.Invoke(player, null);
            if (NVGConfigSection.PodFlipSound.Value) _nvgClickSound?.Invoke(player, null);
        }
        catch (Exception e)
        {
            Plugin.logger!.LogDebug($"[NvgPodRotator] Pod flip feedback failed: {e.InnerException?.Message ?? e.Message}");
        }
    }
        
    private static void TogglePod(ref bool isOverride, ref float targetT, ref float currentT)
    {
        isOverride = !isOverride;
        targetT = isOverride ? 0f : 1f;
        if (!isOverride) currentT = 0f;
    }

    private static float Animate(ref float currentT, ref float targetT, bool isOverride, float mountT)
    {
        if (!isOverride && Mathf.Approximately(currentT, targetT))
        {
            currentT = targetT = mountT;
            return mountT;
        }

        currentT = Mathf.MoveTowards(currentT, targetT, Time.deltaTime * NVGConfigSection.ManualFoldSpeed.Value);
        return currentT;
    }

    public static Transform? FindChildContainingStatic(Transform root, string nameFragment)
    {
        var matches = new List<Transform>();
        CollectChildrenContainingStatic(root, nameFragment, matches);
        foreach (var t in matches)
        {
            if (t.name.Contains("(Clone)"))
            {
                return t;
            }
        }
        return matches.Count > 0 ? matches[0] : null;
    }

    private static void CollectChildrenContainingStatic(Transform root, string nameFragment, List<Transform> results)
    {
        foreach (Transform child in root)
        {
            if (child.name.IndexOf(nameFragment, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                results.Add(child);
            }
            CollectChildrenContainingStatic(child, nameFragment, results);
        }
    }
}