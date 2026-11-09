using BepInEx.Configuration;
namespace WTT_CAG.Client.Config;

public class AMPEditorConfigSection : IConfigSection
{
    internal static ConfigEntry<bool>? AMPPreviewTuning;
    internal static ConfigEntry<float>? AMPEditorGizmoSize;
    
    public void Init(ConfigFile cfg)
    {
        AMPPreviewTuning = cfg.Bind("AMP Arms Editor", "Tune Preview Models", false, new ConfigDescription("Enables the AMP arms preview editor: tunes AMP arm positions in inspect / modding-window previews and turns on the edit keys below. Off removes the preview patch entirely. Close and reopen F12 after changing this to show or hide the editor settings.", null, new CAGConfigurationManagerAttributes { IsAdvanced = true, Order = 100 }));
        AMPEditorGizmoSize = cfg.Bind("AMP Arms Editor", "Gizmo Size", 90f, new ConfigDescription("Length of the gizmo arrows / radius of the rings, in screen pixels.", new AcceptableValueRange<float>(30f, 300f), GetAMPEditorAttributes(60)));
    }
    
    internal static CAGConfigurationManagerAttributes GetAMPEditorAttributes(int order)
    {
        var attr = new CAGConfigurationManagerAttributes
        {
            IsAdvanced = true,
            Browsable = AMPPreviewTuning!.Value,
            Order = order
        };
        
        Plugin.AMPEditorAttributes.Add(attr);
        return attr;
    }
}
