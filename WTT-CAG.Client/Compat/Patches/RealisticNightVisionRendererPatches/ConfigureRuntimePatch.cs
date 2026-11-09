using System;
using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
namespace WTT_CAG.Client.Compat.Patches;

public class ConfigureRuntimePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        var renderer = AccessTools.TypeByName("BorkelRNVG.Controllers.RealisticNightVisionRenderer") ?? throw new TypeLoadException("Borkel renderer was not found");
        var method = AccessTools.Method(renderer, "ConfigureRuntime");

        var parameters = method?.GetParameters();

        if (method == null || parameters == null || parameters.Length < 3 ||
            parameters[1].ParameterType != typeof(Texture) ||
            parameters[1].Name != "lens" ||
            parameters[2].ParameterType != typeof(Texture) ||
            parameters[2].Name != "overlay")
        {
            throw new MissingMethodException("Borkel's ConfigureRuntime(Texture lens, Texture overlay) method was not found.");
        }

        return method;
    }

    [PatchPrefix]
    public static void Prefix(ref Texture __1, ref Texture __2) => BorkelsNVGCompat.ConfigurePrefix(ref __1, ref __2);
}
