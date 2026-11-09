namespace WTT_CAG.Client.Config;

internal sealed class CAGConfigurationManagerAttributes
{
    public bool? ShowRangeAsPercent;
    public bool? Browsable;
    public bool? HideDefaultButton;
    public bool? HideSettingName;
    public bool? ReadOnly;
    public bool? IsAdvanced;
    
    public string? Description;
    public string? DispName;
    public string? Category;
    
    public int? Order;
    
    public object? DefaultValue;
    
    public CustomHotkeyDrawerFunc? CustomHotkeyDrawer;
    public delegate void CustomHotkeyDrawerFunc(BepInEx.Configuration.ConfigEntryBase setting, ref bool isCurrentlyAcceptingInput);
    
    public System.Func<object, string>? ObjToStr;
    public System.Func<string, object>? StrToObj;
    
    public System.Action<BepInEx.Configuration.ConfigEntryBase>? CustomDrawer;
}
