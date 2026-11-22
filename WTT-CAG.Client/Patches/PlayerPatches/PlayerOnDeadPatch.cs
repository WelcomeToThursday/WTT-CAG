using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using WTT_CAG.Client.Content.Thermals.Bodies;

namespace WTT_CAG.Client.Patches.PlayerPatches;

public class PlayerOnDeadPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player), nameof(Player.OnDead));
    }

    [PatchPostfix]
    public static void Postfix(Player __instance)
    {
        if (__instance && !__instance.GetComponent<CorpseCooling>())
        {
            __instance.gameObject.AddComponent<CorpseCooling>();
        }
    }
}
