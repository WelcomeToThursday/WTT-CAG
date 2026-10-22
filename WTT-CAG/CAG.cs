using System.Reflection;
using JetBrains.Annotations;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using WTTServerCommonLib.Models;

namespace WTTClothingAndGear;

[Injectable(TypePriority = OnLoadOrder.TraderRegistration + 3), UsedImplicitly]
public class WTTCAG(WTTServerCommonLib.WTTServerCommonLib wttCommon) : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();

        TraderIds.Add("HOSER", "69eccbae0764116786033c2e");

        await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly);
        await wttCommon.CustomLocaleService.CreateCustomLocales(assembly);
        await wttCommon.CustomBotLoadoutService.CreateCustomBotLoadouts(assembly);
        await wttCommon.CustomClothingService.CreateCustomClothing(assembly);
        await wttCommon.CustomAssortSchemeService.CreateCustomAssortSchemes(assembly);
        await wttCommon.CustomHideoutRecipeService.CreateHideoutRecipes(assembly);
        wttCommon.CustomRigLayoutService.CreateRigLayouts(assembly);
        wttCommon.CustomSlotImageService.CreateSlotImages(assembly);
    }
}
