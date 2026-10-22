using System.Reflection;
using JetBrains.Annotations;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;

namespace WTTClothingAndGear.Traders;

[Injectable(TypePriority = OnLoadOrder.TraderRegistration + 1), UsedImplicitly]
public class WTTCAG_TraderLoad(
    ImageRouter imageRouter,
    TimeUtil timeUtil,
    TraderHelper traderHelper,
    TraderConfig traderConfig,
    RagfairConfig ragfairConfig,
    ModHelper modHelper
) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        var traderImagePath = Path.Combine(pathToMod, "db/TraderHoser/hoser.jpg");

        var traderBase = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "db/TraderHoser/base.jsonc");

        // Create a helper class and use it to register our traders image/icon + set its stock refresh time
        imageRouter.AddRoute(traderBase.Avatar!.Replace(".jpg", ""), traderImagePath);
        TraderHelper.SetTraderUpdateTime(traderConfig, traderBase, timeUtil.GetHoursAsSeconds(1),
            timeUtil.GetHoursAsSeconds(2));

        // Add our trader to the config file, this lets it be seen by the flea market
        ragfairConfig.Traders.TryAdd(traderBase.Id, true);

        // Add our trader (with no items yet) to the server database
        // An 'assort' is the term used to describe the offers a trader sells, it has 3 parts to an assort
        // 1: The item
        // 2: The barter scheme, cost of the item (money or barter)
        // 3: The Loyalty level, what rep level is required to buy the item from trader
        traderHelper.AddTraderWithEmptyAssortToDb(traderBase);

        // Add localisation text for our trader to the database so it shows to people playing in different languages
        traderHelper.AddTraderToLocales(traderBase,
            "A Canadian PMC of unknown allegiance who split from his section during the Blue Fire. Now hiding along the Shoreline, he mostly deals in imported tactical equipment, scavenged from USEC shipping containers or RUAF supply caches.");

        // Get the assort data from JSON
        var assort = modHelper.GetJsonDataFromFile<TraderAssort>(pathToMod, "db/TraderHoser/assort.jsonc");

        // Save the data we loaded above into the trader we've made
        traderHelper.OverwriteTraderAssort(traderBase.Id, assort);

        // Send back a success to the server to say our trader is good to go
        return Task.CompletedTask;
    }
}
