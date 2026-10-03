using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using WTTServerCommonLib.Helpers;

namespace WTTClothingAndGear.Utilities;

[Injectable(typePriority: OnLoadOrder.PostDBModLoader + 3)]
public class BaseGameItemEdits(
    ISptLogger<BaseGameItemEdits> logger,
    DatabaseService databaseService,
    SlotHelper slotHelper
):IOnLoad
{
    public Task OnLoad()
    {
        EditFilters();
        return Task.CompletedTask;
    }

    private void EditFilters()
    {
        var dbItems = databaseService.GetItems();
        foreach (var (id, item) in dbItems)
        {
            switch (id)
            {
                case "5a16b8a9fcdbcb00165aa6ca":
                    ModifySlotFilters(item, 0, 0, [
                        "6974ce066e50d4be623b8d9b",
                        "6974cf52ee1fb8a0683b8d9d"
                    ]);
                    break; //Pushing DTNVGs to TATM mount
                case "689dbded6c7e684817080c29":
                    ModifySlotFilters(item, 0, 0, [
                        "6974ce066e50d4be623b8d9b",
                        "6974cf52ee1fb8a0683b8d9d"
                    ]);
                    break; //Pushing DTNVGs to Black Wilcox
                case "689b8883b49f27df1c0873f8":
                    ModifySlotFilters(item, 0, 0, [
                        "6974ce066e50d4be623b8d9b",
                        "6974cf52ee1fb8a0683b8d9d"
                    ]);
                    break; //Pushing DTNVGs to Tan Wilcox
                case "5f60b34a41e30a4ab12a6947":
                    item.Properties.Prefab.Path = "Headwear/helmets/galvion_caiman/helmet_caiman_bump_grey.bundle";
                    break; // Replacing the Caiman Helmet without overwriting the bundle because i need shit from that bundle lmao
                case "657bbad7a1c61ee0c3036323":
                    item.Properties.ArmorClass = 1;
                    item.Properties.Durability = 10;
                    item.Properties.MaxDurability = 10;
                    break; // Making the Caiman bump shit (Armor Top)
                case "657bbb31b30eca9763051183":
                    item.Properties.ArmorClass = 1;
                    item.Properties.Durability = 10;
                    item.Properties.MaxDurability = 10;
                    break; // Making the Caiman bump shit (Armor Back)
                case "65719f0775149d62ce0a670b":
                    item.Properties.Prefab.Path = "Headwear/helmets/tor-2/item_equipment_helmet_tor_2.bundle"; // Tor-2 Prefab Path
                    slotHelper.EnsureSlot(item, "mod_cover", "55d30c4c4bdc2db4468b457e", false, false, 0);

                    slotHelper.AddIdsToNamedSlot(item, "mod_cover",
                        "69d6dcfb46cc268b92906d4e",
                        "69d6df1e2053bc5e41906d4f",
                        "69d6df883c2d93f229906d51",
                        "69d6dfa9f3b8a5d1b4906d52",
                        "69d6dfc41d822714a7906d53"); // Tor-2 Modslots
                    break;
                case "5b432d215acfc4771e1c6624":
                    item.Properties.Prefab.Path = "Headwear/helmets/lshz/item_equipment_helmet_lshz_highcut.bundle"; // LShZ prefab path
                    slotHelper.EnsureSlot(item, "mod_cover", "55d30c4c4bdc2db4468b457e", false, false, 0);
                    slotHelper.AddIdsToNamedSlot(item, "mod_cover",
                        "6a32bd63cdc9d6712b6ffae0",
                        "6a32b342d54ecde6786ffadf",
                        "6a32bd8954d48c508b6ffae1",
                        "6a32c1e01b484ff5e86ffae2",
                        "6a32c3bfef7e9753a16ffae3"); // LShZ (HC) new slot

                    ModifySlotFilters(item, 0, 0, [
                        "5a16b672fcdbcb001912fa83",
                        "5a16b7e1fcdbcb00165aa6c9"
                    ]); // LShZ removal of side armor
                    break;
                case "544a5caa4bdc2d1a388b4568":
                    item.Properties.Prefab.Path = "Bodywear/armoredRigs/AVS/item_equipment_armor_crye_avs_green.bundle";
                    break; // Replace AVS (Green) bundle
                case "67ab49aab9c7a1e18c095686":
                    item.Properties.Prefab.Path = "Bodywear/armoredRigs/AVS/item_equipment_armor_crye_avs_mc.bundle";
                    break; // Replace AVS (MultiCam) bundle
                case "5b44cad286f77402a54ae7e5":
                    item.Properties.Prefab.Path = "Bodywear/armoredRigs/TACTEC_replace/cr_511_tactec_tan.bundle";
                    break; // Replace TacTec bundle
                case "67ab4b2d6f7ae4aa550bbcf6":
                    item.Properties.Prefab.Path = "Bodywear/armoredRigs/TACTEC_replace/cr_511_tactec_storm.bundle";
                    break; // Replace TacTec bundle (Storm)
            }
        }
    }
    
    private void ReplaceSlotFilters(TemplateItem item, int slotIndex, int filterIndex, HashSet<MongoId> ids)
    {
        var slot = GetSlotAtIndex(item, slotIndex);
        var filter = GetSlotFilterAtIndex(slot, filterIndex);

        filter.Filter = ids;
    }

    private void ModifySlotFilters(TemplateItem item, int slotIndex, int filterIndex, List<MongoId> ids, bool isCartridge = false)
    {
        var slot = GetSlotAtIndex(item, slotIndex, isCartridge);
        var filter = GetSlotFilterAtIndex(slot, filterIndex);

        filter.Filter!.UnionWith(ids);
    }
    
    private Slot GetSlotAtIndex(TemplateItem item, int index, bool isCartridge = false)
    {
        var slots = isCartridge ? item.Properties?.Cartridges?.ToArray() : item.Properties?.Slots?.ToArray();

        if (index >= 0 && index < slots?.Length)
        {
            return slots[index];
        }

        throw new IndexOutOfRangeException($"Index on item slot property `{item.Name}` is out of range");
    }

    private SlotFilter GetSlotFilterAtIndex(Slot slot, int index)
    {  
        var slotFilter = slot.Properties?.Filters?.ToArray() ?? [];

        if (index >= 0 && index < slotFilter.Length)
        {
            return slotFilter[index];
        }

        throw new IndexOutOfRangeException($"Index on slot property `{slot.Name}` is out of range");
    }
}