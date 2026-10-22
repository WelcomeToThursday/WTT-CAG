using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;

namespace WTTClothingAndGear.Traders;

[Injectable(TypePriority = OnLoadOrder.TraderRegistration + 2)]
public class TraderHelper(
    ISptLogger<TraderHelper> logger,
    ICloner cloner,
    TradersTable tradersTable,
    LocaleTable localeTable
)
{
    public static void SetTraderUpdateTime(TraderConfig traderConfig, TraderBase baseJson, int refreshTimeSecondsMin,
        int refreshTimeSecondsMax)
    {
        var traderRefreshRecord = new UpdateTime
        {
            TraderId = baseJson.Id,
            Seconds = new MinMax<int>(refreshTimeSecondsMin, refreshTimeSecondsMax),
        };

        traderConfig.UpdateTime.Add(traderRefreshRecord);
    }

    public void AddTraderWithEmptyAssortToDb(TraderBase traderDetailsToAdd)
    {
        var emptyTraderItemAssortObject = new TraderAssort
        {
            Items = [],
            BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>(),
            LoyalLevelItems = new Dictionary<MongoId, int>()
        };
        var traderDataToAdd = new Trader
        {
            Assort = emptyTraderItemAssortObject,
            Base = cloner.Clone(traderDetailsToAdd)!,
            QuestAssort = new Dictionary<string, Dictionary<MongoId, MongoId>>
            {
                {
                    "Started", new Dictionary<MongoId, MongoId>()
                },
                {
                    "Success", new Dictionary<MongoId, MongoId>()
                },
                {
                    "Fail", new Dictionary<MongoId, MongoId>()
                }
            },
            Dialogue = [],
            Suits = []
        };

        // Add the new trader id and data to the server
        if (!tradersTable.TryAdd(traderDetailsToAdd.Id, traderDataToAdd))
        {
            //Failed to add trader!
        }
    }

    /// <summary>
    /// Add traders name/location/description to all locales (e.g. German/French/English)
    /// </summary>
    /// <param name="baseJson">json file for trader (db/base.json)</param>
    /// <param name="description">Flavor text of whom the trader is</param>
    public void AddTraderToLocales(TraderBase baseJson, string description)
    {
        // For each language, add locale for the new trader
        var locales = localeTable.Global;
        var newTraderId = baseJson.Id;
        var location = baseJson.Location;

        foreach (var (_, localeKvP) in locales)
        {
            // We have to add a transformer here, because locales are lazy loaded due to them taking up huge space in memory
            // The transformer will make sure that each time the locales are requested, the ones added below are included
            localeKvP.AddTransformer(lazyloadedLocaleData =>
            {
                if (location != null)
                {
                    lazyloadedLocaleData?.Add($"{newTraderId} Location", location);
                }

                lazyloadedLocaleData?.Add($"{newTraderId} Description", description);
                return lazyloadedLocaleData;
            });
        }
    }

    /// <summary>
    /// Overwrite the desired traders assorts with the ones provided
    /// </summary>
    /// <param name="traderId">Trader to override assorts of</param>
    /// <param name="newAssorts">new assorts we want to add</param>
    public void OverwriteTraderAssort(string traderId, TraderAssort newAssorts)
    {
        if (!tradersTable.TryGetValue(traderId, out var traderToEdit))
        {
            logger.Warning($"Unable to update assorts for trader: {traderId}, they couldn't be found on the server");

            return;
        }

        // Override the traders assorts with the ones we passed in
        traderToEdit.Assort = newAssorts;
    }
}
