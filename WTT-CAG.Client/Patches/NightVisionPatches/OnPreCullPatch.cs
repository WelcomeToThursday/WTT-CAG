using System.Reflection;
using BSG.CameraEffects;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using WTT_CAG.Client.Content.Thermals;
using WTT_CAG.Client.Content.Thermals.Fusion;
using WTT_CAG.Client.Core.Thermal;
namespace WTT_CAG.Client.Patches.NightVisionPatches;

// The fusion goggles stay a stock NightVision device: BSG's own material, tube mask and noise are
// left alone, and the thermal zones are composited on top of the finished night vision image.
public class OnPreCullPatch : ModulePatch
{
    // NightVision also sits on the pooled optic camera. Attaching there would put a thermal
    // circle in every NVG-capable scope, so only the head camera is touched.
    private const string HostCameraName = "FPS Camera";

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(NightVision), nameof(NightVision.OnPreCull));
    }

    [PatchPrefix]
    public static void Prefix(NightVision __instance)
    {
        if (!ThermalAssets.FusionMaterial) { return; }
        if (!__instance.gameObject.name.Contains(HostCameraName)) { return; }

        var profile = ThermalDeviceProfiles.Active;
        var controller = __instance.GetComponent<FusionThermalController>();

        if (profile == null)
        {
            // Goggles removed. The controller's OnDestroy takes the zone and overlay cameras with it.
            if (controller) { Object.Destroy(controller); }
            return;
        }

        if (!controller)
        {
            __instance.gameObject.AddComponent<FusionThermalController>();
            return;
        }

        var overlay = controller.Overlay;

        if (overlay && overlay!.OverlayMaterial)
        {
            ThermalMaterialProperties.ApplyComposite(overlay.OverlayMaterial!, profile, __instance);
        }
    }

    // A postfix so the composite is always queued after NightVision has refilled its own buffer.
    [PatchPostfix]
    public static void Postfix(NightVision __instance)
    {
        var controller = __instance.GetComponent<FusionThermalController>();

        if (controller && controller.Overlay)
        {
            controller.Overlay!.RebuildCommandBuffer();
        }
    }
}
