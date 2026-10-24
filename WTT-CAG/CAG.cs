using System.ComponentModel;
using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using WTTClothingAndGear.Helpers;
using WTTServerCommonLib.Models;
using Path = System.IO.Path;
using Range = SemanticVersioning.Range;

namespace WTTClothingAndGear;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.wtt.cag";
    public string Name { get; init; } = "WTT-CAG";
    public string Author { get; init; } = "The WTT Team";
    public List<string>? Contributors { get; init; } = null;
    public SemanticVersioning.Version Version { get; init; } = new(typeof(ModMetadata).Assembly.GetName().Version?.ToString(3));
    public Range SptVersion { get; init; } = new("~4.1.6");
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, Range>? ModDependencies { get; init; } = new()
    {
        { "com.wtt.commonlib", new Range("^3.0.6") },
        { "com.wtt.contentbackport", new Range("^2.0.5") }
    };
    public string? Url { get; init; }
    public bool HasPrepatcher { get; init; } = false;
    public string License { get; init; } = "CC-BY-NC-ND 4.0";
}


[Injectable(TypePriority = OnLoadOrder.Preload + 3)]
public class WTTClothingAndGear(
    WTTServerCommonLib.WTTServerCommonLib wttCommon,
    CagQuestHelper cagQuestHelper
) : IOnLoad 

{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        
        TraderIds.Add("HOSER", "69eccbae0764116786033c2e");
        
        await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly);
        await wttCommon.CustomLocaleService.CreateCustomLocales(assembly);
        await wttCommon.CustomBotLoadoutService.CreateCustomBotLoadouts(assembly);
        await wttCommon.CustomClothingService.CreateCustomClothing(assembly); 
        await wttCommon.CustomAssortSchemeService.CreateCustomAssortSchemes(assembly);
        wttCommon.CustomRigLayoutService.CreateRigLayouts(assembly); 
        wttCommon.CustomSlotImageService.CreateSlotImages(assembly);
        await wttCommon.CustomHideoutRecipeService.CreateHideoutRecipes(assembly);
        cagQuestHelper.ModifyQuests();
    }
}
