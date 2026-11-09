using System.Collections.Generic;
using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using SPT.Reflection.Patching;
using UnityEngine;
using WTT_CAG.Client.AMPArms;
using WTT_CAG.Client.Compat;
using WTT_CAG.Client.Config;
using WTT_CAG.Client.Content.Thermals;
using WTT_CAG.Client.Core;
using WTT_CAG.Client.Patches.CurveRotatorPatches;
using WTT_CAG.Client.Patches.NightVisionPatches;
using WTT_CAG.Client.Patches.ThermalVisionPatches;

namespace WTT_CAG.Client;

[BepInPlugin(PluginConstants.Guid, PluginConstants.Name, PluginConstants.Version)]
[BepInDependency(BorkelsNVGCompat.GUID, BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource? logger;
    private static ConfigFile? _configFile;
    internal static string? PluginFolder;

    internal static Dictionary<string, NVGDeviceProfile> Profiles = new();
    internal static Dictionary<string, HelmetSlotConfigSection> HelmetConfigs = new();
    internal static readonly List<CAGConfigurationManagerAttributes> AMPEditorAttributes = [];

    internal static AssetBundle? Bundle;
    
    internal static void RegisterHelmet(HelmetSlotConfigSection section)
    {
        ((IConfigSection)section).Init(_configFile!);
        HelmetConfigs.Add(section.HelmetTpl!, section);
    }

    internal static void RegisterDevice(NVGDeviceDefinition definition)
    {
        Profiles[definition.PrefabNameContains] = new NVGDeviceProfile(definition, _configFile!);
    }

    private void Awake()
    {
        logger = Logger;
        _configFile = Config;
        PluginFolder = Path.GetDirectoryName(Info.Location);

        new NVGConfigSection().Init(Config);
        new KeyboardConfigSection().Init(Config);
        new ThermalConfigSection().Init(Config);

        if (BorkelsNVGCompat.TryEnable())
        {
            logger.LogInfo("[WTT-CAG] Borkel's NVG compatibility patch successfully enabled.");
        }

        EnablePatch(new SetPatch());
        EnablePatch(new ApplySettingsPatch());
        EnablePatch(new SetMaterialPropertiesPatch());

        if (Profiles.Count == 0)
        {
            logger.LogWarning("[WTT-CAG] No NVG device profiles registered; pod rotation will remain inactive.");
        }
    }

    private void Update()
    {
        ThermalDeviceProfiles.RefreshActive();

        if (KeyboardConfigSection.ResetRotationsKey?.Value.IsDown() == true)
        {
            foreach (var profile in Profiles.Values)
            {
                profile.ResetToDefaults();
            }

            logger!.LogInfo("[WTT-CAG] NVG rotations reset to defaults.");
        }

        if (KeyboardConfigSection.CopyConfigKey?.Value.IsDown() != true) { return; }
        logger!.LogInfo("===== HELMET CONFIG SNAPSHOT =====");

        foreach (var section in HelmetConfigs.Values)
        {
            logger.LogInfo(section.ToCSharpSnippet());
        }

        logger.LogInfo("==================================");
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
