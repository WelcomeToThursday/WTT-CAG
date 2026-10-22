using JetBrains.Annotations;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace WTTClothingAndGear.Utilities;

[Injectable(TypePriority = OnLoadOrder.PostLoad + 4), UsedImplicitly]
public class QuestEquipmentHelper(TemplateTable templateTable) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        ModifyQuests();
        return Task.CompletedTask;
    }

    private void ModifyQuests()
    {
        var quests = templateTable.Quests;

        AddArmorToEquipmentExclusive(quests[QuestTpl.THE_SURVIVALIST_PATH_UNPROTECTED_BUT_DANGEROUS], ModItems.ARMORS);
        AddArmorToEquipmentExclusive(quests[QuestTpl.SWIFT_ONE], ModItems.ARMORS.With([ModItems.HELMETS, ModItems.ARMORED_FACECOVERS]));
        AddArmorToEquipmentInclusive(quests[QuestTpl.PEACEKEEPING_MISSION], ModItems.UNTAR);
        AddArmorToEquipmentInclusive(quests[QuestTpl.HUMANITARIAN_SUPPLIES], ModItems.UNTAR);
        foreach (var maskId in ModItems.GAS_MASKS)
        {
            AddArmorToEquipmentInclusive(quests[QuestTpl.DECONTAMINATION_SERVICE], [maskId]);
        }
    }

    private static void AddArmorToEquipmentExclusive(Quest quest, List<string> armorIds)
    {
        if (quest.Conditions.AvailableForFinish == null)
        {
            return;
        }

        foreach (var condition in quest.Conditions.AvailableForFinish)
        {
            if (condition is not { ConditionType: "CounterCreator", Counter.Conditions: not null })
            {
                continue;
            }

            foreach (var counterCondition in condition.Counter.Conditions)
            {
                if (counterCondition is not { ConditionType: "Equipment", EquipmentExclusive: not null })
                {
                    continue;
                }

                foreach (var armorId in armorIds)
                {
                    counterCondition.EquipmentExclusive.Add([armorId]);
                }
            }
        }
    }

    private static void AddArmorToEquipmentInclusive(Quest quest, List<string>? armorIds)
    {
        if (armorIds == null || armorIds.Count == 0 || quest.Conditions.AvailableForFinish == null)
        {
            return;
        }

        foreach (var condition in quest.Conditions.AvailableForFinish)
        {
            if (condition is not { ConditionType: "CounterCreator", Counter.Conditions: not null })
            {
                continue;
            }

            foreach (var counterCondition in condition.Counter.Conditions)
            {
                if (counterCondition is not { ConditionType: "Equipment", EquipmentInclusive: not null })
                {
                    continue;
                }

                var equipmentInclusive = counterCondition.EquipmentInclusive.ToList();
                equipmentInclusive.Add([.. armorIds]);

                counterCondition.EquipmentInclusive = equipmentInclusive;
            }
        }
    }
}
