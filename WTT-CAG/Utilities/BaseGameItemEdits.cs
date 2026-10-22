using JetBrains.Annotations;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;

namespace WTTClothingAndGear.Utilities;

[Injectable(TypePriority = OnLoadOrder.PostLoad + 3), UsedImplicitly]
public class BaseGameItemEdits(TemplateTable templateTable, SlotHelper slotHelper) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        EditFilters();
        return Task.CompletedTask;
    }

    private void EditFilters()
    {
        var dbItems = templateTable.Items;

        foreach (var (id, item) in dbItems)
        {
            switch (id)
            {
                // DTNVG -> TATM and ContentBackport Wilcox L4G24 mounts
                case "689dbded6c7e684817080c29": // Black Wilcox
                case "689b8883b49f27df1c0873f8": // Tan Wilcox
                case var _ when id == ItemTpl.MOUNT_NOROTOS_TITANIUM_ADVANCED_TACTICAL:
                {
                    ModifySlotFilters(item, 0, 0, [
                        "6974ce066e50d4be623b8d9b",
                        "6974cf52ee1fb8a0683b8d9d"
                    ]);

                    break;
                }

                // REPLACE CAIMAN WITHOUT BUNDLE OVERWRITE
                case var _ when id == ItemTpl.HEADWEAR_GALVION_CAIMAN_HYBRID_HELMET_GREY:
                {
                    item.Properties!.Prefab!.Path = "Headwear/helmets/galvion_caiman/helmet_caiman_bump_grey.bundle";
                    break;
                }

                case var _ when id == ItemTpl.BUILTININSERTS_GALVION_CAIMAN_LEVEL3_HELMET_ARMOR_TOP: // CAIMAN BUMP TOP ARMOR
                case var _ when id == ItemTpl.BUILTININSERTS_GALVION_CAIMAN_LEVEL3_HELMET_ARMOR_NAPE: // CAIMAN BUMP BACK ARMOR
                {
                    item.Properties!.ArmorClass = 1;
                    item.Properties.Durability = 10;
                    item.Properties.MaxDurability = 10;
                    break;
                }

                case var _ when id == ItemTpl.HEADWEAR_NPP_KLASS_TOR2_HELMET_OLIVE_DRAB:
                {
                    // REPLACE TOR-2 BUNDLE
                    item.Properties!.Prefab!.Path = "Headwear/helmets/tor-2/item_equipment_helmet_tor_2.bundle";

                    // ADD NEW TOR-2 MOD SLOTS
                    slotHelper.EnsureSlot(item, "mod_cover", "55d30c4c4bdc2db4468b457e");
                    slotHelper.AddIdsToNamedSlot(item, "mod_cover",
                        "69d6dcfb46cc268b92906d4e",
                        "69d6df1e2053bc5e41906d4f",
                        "69d6df883c2d93f229906d51",
                        "69d6dfa9f3b8a5d1b4906d52",
                        "69d6dfc41d822714a7906d53"
                    );

                    break;
                }

                case var _ when id == ItemTpl.HEADWEAR_LSHZ_LIGHTWEIGHT_HELMET_OLIVE_DRAB:
                {
                    // REPLACE LZSH BUNDLE
                    item.Properties!.Prefab!.Path = "Headwear/helmets/lshz/item_equipment_helmet_lshz_highcut.bundle";

                    // ADD NEW LZSH MOD SLOTS
                    slotHelper.EnsureSlot(item, "mod_cover", "55d30c4c4bdc2db4468b457e");
                    slotHelper.AddIdsToNamedSlot(item, "mod_cover",
                        "6a32bd63cdc9d6712b6ffae0",
                        "6a32b342d54ecde6786ffadf",
                        "6a32bd8954d48c508b6ffae1",
                        "6a32c1e01b484ff5e86ffae2",
                        "6a32c3bfef7e9753a16ffae3"
                    );

                    // Keep the visor options, removing side armor from this slot.
                    ReplaceSlotFilters(item, 0, 0, [
                        ItemTpl.ARMOREDEQUIPMENT_OPSCORE_FAST_VISOR,
                        ItemTpl.ARMOREDEQUIPMENT_OPSCORE_FAST_MULTIHIT_BALLISTIC_FACE_SHIELD
                    ]);

                    break;
                }
            }
        }
    }

    private static void ReplaceSlotFilters(TemplateItem item, int slotIndex, int filterIndex, HashSet<MongoId> ids)
    {
        var slot = GetSlotAtIndex(item, slotIndex);
        var filter = GetSlotFilterAtIndex(slot, filterIndex);
        filter.Filter = ids;
    }

    private static void ModifySlotFilters(TemplateItem item, int slotIndex, int filterIndex, List<MongoId> ids, bool isCartridge = false)
    {
        var slot = GetSlotAtIndex(item, slotIndex, isCartridge);
        var filter = GetSlotFilterAtIndex(slot, filterIndex);

        filter.Filter!.UnionWith(ids);
    }

    private static Slot GetSlotAtIndex(TemplateItem item, int index, bool isCartridge = false)
    {
        var slots = isCartridge ? item.Properties?.Cartridges?.ToArray() : item.Properties?.Slots?.ToArray();

        if (index >= 0 && index < slots?.Length)
        {
            return slots[index];
        }

        throw new IndexOutOfRangeException($"Index on item slot property `{item.Name}` is out of range");
    }

    private static SlotFilter GetSlotFilterAtIndex(Slot slot, int index)
    {
        var slotFilter = slot.Properties?.Filters?.ToArray() ?? [];

        if (index >= 0 && index < slotFilter.Length)
        {
            return slotFilter[index];
        }

        throw new IndexOutOfRangeException($"Index on slot property `{slot.Name}` is out of range");
    }
}
