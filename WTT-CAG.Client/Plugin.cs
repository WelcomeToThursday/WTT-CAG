using BepInEx;

namespace WTT_CAG.Client;

[BepInPlugin("com.wtt.cag", "WTT-CAG", "1.1.0-dev")]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        var version = typeof(Plugin).Assembly.GetName().Version!.ToString(3);
        Logger.LogInfo($"WTT-CAG v{version} loaded.");
    }
}