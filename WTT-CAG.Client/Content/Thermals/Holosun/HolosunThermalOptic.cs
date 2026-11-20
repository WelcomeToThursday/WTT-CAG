using WTT_CAG.Client.Core;
using Comfort.Common;
using EFT;
using UnityEngine;
using WTT_CAG.Client.Core.Thermal;
using WTT_CAG.Client.Patches.ThermalVisionPatches;

namespace WTT_CAG.Client.Content.Thermals.Holosun;

// Lives on the pooled optic camera while the Holosun is the aimed sight.
public class HolosunThermalOptic : MonoBehaviour
{
    public const string TemplateId = "b0b2840b9daf26136be3884c";

    private const string OpticCameraName = "BaseOpticCamera";
    private const int RedDotScopeMode = 3;

    private ThermalVision? _thermalVision;
    private Material? _material;
    private bool _thermalSuppressed;

    public static bool IsAimed
    {
        get
        {
            var sight = Singleton<GameWorld>.Instance?.MainPlayer?.ProceduralWeaponAnimation?.CurrentAimingMod;

            return sight != null && sight.Item.StringTemplateId == TemplateId;
        }
    }

    public static int ScopeMode => Singleton<GameWorld>.Instance?.MainPlayer?.ProceduralWeaponAnimation?.CurrentAimingMod?.SelectedScopeMode ?? 0;

    public static bool TryAttach(ThermalVision instance)
    {
        if (!ThermalAssets.TraceMaterial) { return false; }
        if (!instance.gameObject.name.Contains(OpticCameraName)) { return false; }
        if (!IsAimed) { return false; }
        if (instance.GetComponent<HolosunThermalOptic>()) { return false; }

        instance.gameObject.AddComponent<HolosunThermalOptic>();

        return true;
    }

    public static void Detach(ThermalVision instance)
    {
        var optic = instance.GetComponent<HolosunThermalOptic>();

        if (optic) { Destroy(optic); }
    }

    private void Awake()
    {
        _thermalVision = GetComponent<ThermalVision>();

        if (!_thermalVision || !ThermalAssets.TraceMaterial) { return; }

        _material = new Material(ThermalAssets.TraceMaterial) { name = $"{ThermalAssets.TraceMaterial!.name}_{name}_Instance" };

        SetMaterialPropertiesPatch.Register(_thermalVision!, _material, EThermalDeviceType.HOLOSUN, _ => IsAimed);
    }

    // Red dot mode switches the effect itself off. Done here because SetMaterialProperties stops being
    // called once the effect is off, so the patch could never switch it back on.
    private void Update()
    {
        if (!_thermalVision) { return; }

        var redDot = ScopeMode == RedDotScopeMode;

        if (redDot == _thermalSuppressed) { return; }

        _thermalSuppressed = redDot;
        _thermalVision!.On = !redDot;

        CagLog.Info($"[WTT-CAG] Holosun thermal {(redDot ? "off - red dot mode" : "on")}.");
    }

    private void OnDestroy()
    {
        if (_thermalVision)
        {
            if (_thermalSuppressed) { _thermalVision!.On = true; }

            SetMaterialPropertiesPatch.Unregister(_thermalVision!);
        }

        if (_material)
        {
            Destroy(_material);
            _material = null;
        }
    }
}
