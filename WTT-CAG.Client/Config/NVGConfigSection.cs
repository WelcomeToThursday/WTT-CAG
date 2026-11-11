using BepInEx.Configuration;
namespace WTT_CAG.Client.Config;

public class NVGConfigSection : IConfigSection
{
    private readonly CAGConfigurationManagerAttributes _advancedUsage = new() { IsAdvanced = true };

    internal static ConfigEntry<float>? ManualFoldSpeed;
    internal static ConfigEntry<bool>? PodFlipAnimation;
    internal static ConfigEntry<bool>? PodFlipSound;

    public void Init(ConfigFile cfg)
    {
        ManualFoldSpeed = cfg.Bind("General", "Manual Fold Speed", 4f, new ConfigDescription("Pod animation speed", new AcceptableValueRange<float>(0.5f, 10f), _advancedUsage));
        PodFlipAnimation = cfg.Bind("General", "Pod Flip Animation", true, "Plays the character's reach-to-helmet animation when a pod key is pressed.");
        PodFlipSound = cfg.Bind("General", "Pod Flip Sound", true, "Plays the NVG click when a pod key is pressed.");
    }
}
