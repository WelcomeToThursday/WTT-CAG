using System.Reflection;
using BSG.CameraEffects;
using HarmonyLib;
using SPT.Reflection.Patching;
using WTT_CAG.Client.Compat;
using WTT_CAG.Client.Core;

namespace WTT_CAG.Client.Patches.NightVisionPatches;

public class ApplySettingsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(NightVision), nameof(NightVision.ApplySettings));
    }

    [PatchPrefix]
    private static void Prefix(NightVision __instance)
    {
        if (__instance.TextureMask == null || BorkelsNVGCompat.Active || !NVGPodRotator.ActiveMask)
        {
            return;
        }

        if (NVGPodRotator.ManagedInstance == null || __instance == NVGPodRotator.ManagedInstance)
        {
            __instance.Mask = NVGPodRotator.ActiveMask;
        }
    }
}
