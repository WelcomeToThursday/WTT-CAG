using System.Collections.Generic;
using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using SPT.Reflection.Patching;
using UnityEngine;
using WTT_CAG.Client.AMPArms;
using WTT_CAG.Client.Config;
using WTT_CAG.Client.Content.Thermals;
using WTT_CAG.Client.Content.Thermals.Fusion;
using WTT_CAG.Client.Core;
using WTT_CAG.Client.Core.Thermal;
using WTT_CAG.Client.Patches.NightVisionPatches;
using WTT_CAG.Client.Patches.ThermalVisionPatches;

namespace WTT_CAG.Client;

[BepInPlugin(PluginConstants.Guid, PluginConstants.Name, PluginConstants.Version)]
public class Plugin : BaseUnityPlugin
{
    private const string ThermalBundleName = "ThermalTraceOverlay.bundle";
    private const string HudBundleName = "thermalhud.bundle";

    internal static ManualLogSource? logger;
    private static ConfigFile? _configFile;
    internal static string? PluginFolder;

    internal static Dictionary<string, HelmetSlotConfigSection> HelmetConfigs = new();
    internal static readonly List<CAGConfigurationManagerAttributes> AMPEditorAttributes = [];

    // Kept open after startup so device overlay textures can be loaded by name later.
    internal static AssetBundle? Bundle;

    internal static void RegisterHelmet(HelmetSlotConfigSection section)
    {
        ((IConfigSection)section).Init(_configFile!);
        HelmetConfigs.Add(section.HelmetTpl!, section);
    }

    private void Awake()
    {
        logger = Logger;
        _configFile = Config;
        PluginFolder = Path.GetDirectoryName(Info.Location);

        new KeyboardConfigSection().Init(Config);
        new ThermalConfigSection().Init(Config);

        // The thermal patches do nothing without their shaders, so they are only enabled once the
        // bundle has loaded. A missing bundle does not stop the rest of the plugin.
        if (LoadThermalAssets())
        {
            EnablePatch(new SetMaterialPropertiesPatch());
            EnablePatch(new OnPreCullPatch());
        }
    }

    private void Update()
    {
        ThermalDeviceProfiles.RefreshActive();

        if (KeyboardConfigSection.ThermalToggleKey?.Value.IsDown() == true) { FusionThermalState.ToggleThermal(); }
        if (KeyboardConfigSection.ThermalModeKey?.Value.IsDown() == true) { FusionThermalState.CycleMode(); }

        if (KeyboardConfigSection.CopyConfigKey?.Value.IsDown() != true) { return; }
        logger!.LogInfo("===== HELMET CONFIG SNAPSHOT =====");

        foreach (var section in HelmetConfigs.Values)
        {
            logger.LogInfo(section.ToCSharpSnippet());
        }

        logger.LogInfo("==================================");
    }

    private static bool LoadThermalAssets()
    {
        var bundlePath = Path.Combine(PluginFolder!, ThermalBundleName);

        if (!File.Exists(bundlePath))
        {
            logger!.LogError($"[WTT-CAG] {ThermalBundleName} not found. Expected: {bundlePath}");
            return false;
        }

        Bundle = AssetBundle.LoadFromFile(bundlePath);

        if (!Bundle)
        {
            logger!.LogError($"[WTT-CAG] Found {ThermalBundleName} but Unity failed to load it. Check it was built for the correct Unity version.");
            return false;
        }

        ThermalAssets.Load(Bundle!);

        LoadHudBundle();

        return true;
    }

    // The HUD bundle is optional. Without it the goggles work, just with no HUD.
    private static void LoadHudBundle()
    {
        var bundlePath = Path.Combine(PluginFolder!, HudBundleName);

        if (!File.Exists(bundlePath))
        {
            logger!.LogWarning($"[WTT-CAG] {HudBundleName} not found - goggles will have no HUD. Expected: {bundlePath}");
            return;
        }

        ThermalAssets.HudBundle = AssetBundle.LoadFromFile(bundlePath);

        if (!ThermalAssets.HudBundle)
        {
            logger!.LogError($"[WTT-CAG] Found {HudBundleName} but Unity failed to load it. Check it was built for the correct Unity version.");
            return;
        }

        logger!.LogInfo($"[WTT-CAG] Loaded {HudBundleName}.");
    }

    private static void EnablePatch(ModulePatch patch)
    {
        try
        {
            patch.Enable();
            logger!.LogDebug($"[WTT-CAG] Enabled {patch.GetType().Name}.");
        }
        catch (Exception e)
        {
            logger!.LogError($"[WTT-CAG] Failed to enable {patch.GetType().Name}: {e}");
        }
    }
}
