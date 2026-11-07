using BepInEx;
using BepInEx.Logging;
namespace WTT_CAG.Client;

[BepInPlugin(PluginConstants.Guid, PluginConstants.Name, PluginConstants.Version)]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource? logger;
    
    private void Awake()
    {
        logger = Logger;
    }
}
