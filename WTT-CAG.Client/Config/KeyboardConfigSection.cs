using BepInEx.Configuration;
using UnityEngine;
namespace WTT_CAG.Client.Config;

public class KeyboardConfigSection : IConfigSection
{
    private readonly CAGConfigurationManagerAttributes _advancedUsage = new() { IsAdvanced = true };
    
    internal static ConfigEntry<KeyboardShortcut>? BothPodsFoldKey;
    internal static ConfigEntry<KeyboardShortcut>? LeftPodFoldKey;
    internal static ConfigEntry<KeyboardShortcut>? RightPodFoldKey;
    internal static ConfigEntry<KeyboardShortcut>? ResetRotationsKey;
    internal static ConfigEntry<KeyboardShortcut>? CopyConfigKey;
    internal static ConfigEntry<KeyboardShortcut>? ScanMissingScriptsKey;
    internal static ConfigEntry<KeyboardShortcut>? AmpEditKey;
    internal static ConfigEntry<KeyboardShortcut>? AmpEditToolKey;
    internal static ConfigEntry<KeyboardShortcut>? AmpEditPartKey;
    
    public void Init(ConfigFile cfg)
    {
        BothPodsFoldKey = cfg.Bind("Keyboard", "Fold/Unfold Both Pods", new KeyboardShortcut(KeyCode.P), "Toggle both pods. Both folded = NVG powers off.");
        LeftPodFoldKey = cfg.Bind("Keyboard", "Fold/Unfold Left Pod", new KeyboardShortcut(KeyCode.P, KeyCode.LeftShift), "Toggle left pod only.");
        RightPodFoldKey = cfg.Bind("Keyboard", "Fold/Unfold Right Pod", new KeyboardShortcut(KeyCode.P, KeyCode.LeftControl), "Toggle right pod only.");
        ResetRotationsKey = cfg.Bind("Keyboard", "Reset Rotations To Default", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Press to restore all rotation values to defaults.", null, _advancedUsage));
        CopyConfigKey = cfg.Bind("Keyboard", "Copy Helmet Configs To Log", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Prints current F12 helmet slot values to BepInEx console as copy-pasteable C# code.", null, _advancedUsage));
        ScanMissingScriptsKey = cfg.Bind("Keyboard", "Scan For Missing Scripts", new KeyboardShortcut(KeyCode.F8), new ConfigDescription("Dumps every GameObject with an unresolved script component.", null, _advancedUsage));
    }
}
