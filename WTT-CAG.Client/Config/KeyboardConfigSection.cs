using BepInEx.Configuration;
using UnityEngine;
namespace WTT_CAG.Client.Config;

public class KeyboardConfigSection : IConfigSection
{
    private readonly CAGConfigurationManagerAttributes _advancedUsage = new() { IsAdvanced = true };
    

   // internal static ConfigEntry<KeyboardShortcut>? CopyConfigKey;
    // internal static ConfigEntry<KeyboardShortcut>? AmpEditKey;
    // internal static ConfigEntry<KeyboardShortcut>? AmpEditToolKey;
    // internal static ConfigEntry<KeyboardShortcut>? AmpEditPartKey;

    internal static ConfigEntry<KeyboardShortcut>? ThermalModeKey;
    
    public void Init(ConfigFile cfg)
    {
        //CopyConfigKey = cfg.Bind("Keyboard", "Copy Helmet Configs To Log", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Prints current F12 helmet slot values to BepInEx console as copy-pasteable C# code.", null, _advancedUsage));

        ThermalModeKey = cfg.Bind("Keyboard", "Fusion Goggle Thermal Mode", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Steps the worn goggle through its thermal modes. On the fused goggles the last step switches the thermal off."));
    }
}