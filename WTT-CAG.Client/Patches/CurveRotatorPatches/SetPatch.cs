using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;
using WTT_CAG.Client.Core;
namespace WTT_CAG.Client.Patches.CurveRotatorPatches;

public class SetPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(CurveRotator), nameof(CurveRotator.Set));
    }

    [PatchPostfix]
    private static void PatchPostfix(CurveRotator __instance, bool isOn, bool initial)
    {
        NVGDeviceProfile? profile = null;

        foreach (var kvp in Plugin.Profiles)
        {
            var p = kvp.Value;

            if (!__instance.gameObject.name.Contains(p.PrefabNameContains))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(p.ChildNVGName) && !NVGPodRotator.FindChildContainingStatic(__instance.transform, p.ChildNVGName))
            {
                continue;
            }
            
            profile = p;
            break;
        }

        if (profile == null)
        {
            return;
        }

        var rotator = __instance.gameObject.GetComponent<NVGPodRotator>();

        if (!rotator)
        {
            rotator = __instance.gameObject.AddComponent<NVGPodRotator>();
            rotator.Init(__instance, profile);
        }
        
        rotator.OnMountStateChanged(isOn, initial);
    }
}
