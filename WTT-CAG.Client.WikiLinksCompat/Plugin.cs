using BepInEx;
using BepInEx.Bootstrap;

namespace WTT_CAG.Client.WikiLinksCompat;

[BepInPlugin(PluginConstants.Guid, PluginConstants.Name, PluginConstants.Version)]
[BepInDependency("com.tyfon.wikilinks", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        if (!Chainloader.PluginInfos.ContainsKey("com.tyfon.wikilinks")) { return; }

        WikiLinks.RedirectRegistry.AddWikiRedirect("", "");
    }
}