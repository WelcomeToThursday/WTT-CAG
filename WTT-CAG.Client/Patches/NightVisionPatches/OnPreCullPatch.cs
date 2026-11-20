using System.Reflection;
using BSG.CameraEffects;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using WTT_CAG.Client.Content.Thermals;
using WTT_CAG.Client.Content.Thermals.Fusion;
using WTT_CAG.Client.Core.Thermal;
namespace WTT_CAG.Client.Patches.NightVisionPatches;

public class OnPreCullPatch : ModulePatch
{
    // NightVision also sits on the pooled optic camera; only the head camera is used.
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
