using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WTT_CAG.Client.Core.Thermal;

public static class ThermalAssets
{
    // Loaded by file name, not full path, so the shaders can be moved around the Unity project
    // without breaking the plugin.
    private const string TraceShaderName = "ThermalTraceOverlay";
    private const string FusionShaderName = "FusionNVG";

    // ThermalTraceOverlay - shared by the Holosun optic and the goggles' thermal zone camera.
    public static Material? TraceMaterial { get; private set; }

    // FusionNVG - composites the zone camera's render over the night vision image.
    public static Material? FusionMaterial { get; private set; }

    // Properties each shader's source declares. A compiled shader missing any of them was built
    // from an older source than this plugin expects, and will ignore the settings it does not know.
    private static readonly string[] TraceProperties = ["_TraceThreshold", "_OutlineIntensity", "_HotColor", "_ColdColor", "_HeatPower", "_HeatGain", "_HeatFloor", "_DebugHeat"];
    private static readonly string[] FusionProperties = ["_ThermalZoneEnabled", "_ZoneCount", "_ThermalZoneUvScale", "_ThermalZoneZoom", "_HeatKeyOnly", "_OverlayTex", "_ZoneDebugFill", "_ZoneShape"];

    // Holds the HUD prefabs. Optional: without it the goggles simply have no HUD.
    public static AssetBundle? HudBundle;

    public static GameObject? LoadHudPrefab(string prefabName)
    {
        if (!HudBundle) { return null; }

        var prefab = LoadByName<GameObject>(HudBundle!, prefabName);

        if (!prefab)
        {
            Plugin.logger!.LogWarning($"[WTT-CAG] HUD prefab '{prefabName}' not found in the HUD bundle. In it: {string.Join(", ", HudBundle.GetAllAssetNames())}");
        }

        return prefab;
    }

    // Loads an asset by its file name, wherever it sits in the Unity project. Bundles built the
    // standard way answer to the bare name; bundles from the Scriptable Build Pipeline only answer
    // to the full path they were built with. So the bare name is tried first, then the full path
    // is looked up from the bundle's own asset list.
    private static T? LoadByName<T>(AssetBundle bundle, string assetName) where T : UnityEngine.Object
    {
        var asset = bundle.LoadAsset<T>(assetName);

        if (asset) { return asset; }

        foreach (var path in bundle.GetAllAssetNames())
        {
            if (!string.Equals(Path.GetFileNameWithoutExtension(path) ?? "", assetName, StringComparison.OrdinalIgnoreCase)) { continue; }

            asset = bundle.LoadAsset<T>(path);

            if (asset) { return asset; }
        }

        return null;
    }

    public static void Load(AssetBundle bundle)
    {
        TraceMaterial = BuildMaterial(bundle, TraceShaderName, "ThermalTraceOverlayMat_Runtime", TraceProperties);
        FusionMaterial = BuildMaterial(bundle, FusionShaderName, "FusionNVGMat_Runtime", FusionProperties);
    }

    private static Material? BuildMaterial(AssetBundle bundle, string shaderName, string materialName, string[] expectedProperties)
    {
        var shader = LoadByName<Shader>(bundle, shaderName);

        if (!shader)
        {
            Plugin.logger!.LogError($"[WTT-CAG] Shader '{shaderName}' not found in bundle. In it: {string.Join(", ", bundle.GetAllAssetNames())}");
            return null;
        }

        Plugin.logger!.LogInfo($"[WTT-CAG] Loaded shader '{shader.name}'.");

        var material = new Material(shader) { name = materialName };
        var missing = new List<string>();

        foreach (var property in expectedProperties)
        {
            if (!material.HasProperty(property)) { missing.Add(property); }
        }

        if (missing.Count > 0)
        {
            Plugin.logger!.LogError($"[WTT-CAG] Shader '{shader.name}' in the bundle is out of date - it does not declare: {string.Join(", ", missing)}. Rebuild the bundle from the current shader source.");
        }

        return material;
    }
}
