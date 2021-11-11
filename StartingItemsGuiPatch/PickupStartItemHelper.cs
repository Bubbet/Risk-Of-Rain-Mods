using System;
using System.Collections.Generic;
using System.Linq;
using Phedg1Studios.StartingItemsGUI;
using RoR2;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StartingItemsGuiPatch
{
    public static class PickupStartItemHelper
    {
        private static Dictionary<PlayerCharacterMasterController, uint> masterMap = new Dictionary<PlayerCharacterMasterController, uint>();

        public static bool MatchItem(EquipmentDef eq, int item)
        {
            if (eq == RoR2Content.Equipment.QuestVolatileBattery) return true; // april egg
            
            var item_ind = PickupCatalog.GetPickupDef(FindPickupIndex(item)).equipmentIndex;
            if (item_ind == EquipmentIndex.None) return false;
            var item_eq = EquipmentCatalog.GetEquipmentDef(item_ind);
            if (eq.isBoss & !item_eq.isBoss) return false;
            if (eq.isLunar & !item_eq.isLunar) return false;
            return true;
        }

        public static bool MatchItem(ItemDef it, int item)
        {
            var item_ind = PickupCatalog.GetPickupDef(FindPickupIndex(item)).itemIndex;
            if (item_ind == ItemIndex.None) return false;
            return it.tier == ItemCatalog.GetItemDef(item_ind).tier;
        }

        public static PickupIndex FindPickupIndex(int item)
        {
            if (Data.allItemIDs.ContainsKey(item))
            {
                return PickupCatalog.FindPickupIndex(Data.allItemIDs[item]);
            }
            if (Data.allEquipmentIDs.ContainsKey(item))
            {
                return PickupCatalog.FindPickupIndex(Data.allEquipmentIDs[item]);
            }

            return default;
        }

        public static bool AnyoneHas(PickupDef pickupDef)
        {
            ItemDef def = null;
            EquipmentDef eqdef = null;

            
            if (pickupDef.itemIndex != ItemIndex.None) def = ItemCatalog.GetItemDef(pickupDef.itemIndex); 
            if (pickupDef.equipmentIndex != EquipmentIndex.None) eqdef = EquipmentCatalog.GetEquipmentDef(pickupDef.equipmentIndex);
            foreach (var tup in GameManager.items)
            {
                var who = NetworkUser.readOnlyInstancesList.FirstOrDefault(x => tup.Key == x.netId.Value);
                if (who == default) continue;
                if (!who.isParticipating || who.master.IsDeadAndOutOfLivesServer()) continue;
                
                foreach (var tupe in tup.Value)
                {
                    var item = tupe.Key;
                    var amount = tupe.Value;
                    if(amount <= 0) continue;
                    if (def != null)
                    {
                        if (!StartingItemsGuiPatch.ItemsOverTimeMixTier.Value || MatchItem(def, item)) return true;
                    }
                    if (eqdef != null)
                    {
                        if (!StartingItemsGuiPatch.ItemsOverTimeMixTier.Value || MatchItem(eqdef, item)) return true;
                    }
                }
            }
            return false;
        }

        // returns true if any player has an item in that tier
        public static bool NearPrinter(GenericPickupController.CreatePickupInfo createPickupInfo)
        {
            var purchaseInteractions = Object.FindObjectsOfType<PurchaseInteraction>();

            // this is terrible
            return purchaseInteractions.Any(x => (x.costType == CostTypeIndex.Equipment ||
                                                  x.costType == CostTypeIndex.WhiteItem ||
                                                  x.costType == CostTypeIndex.GreenItem ||
                                                  x.costType == CostTypeIndex.BossItem ||
                                                  x.costType == CostTypeIndex.RedItem ||
                                                  x.costType == CostTypeIndex.LunarItemOrEquipment) &&
                                                 Vector3.Distance(x.transform.position,
                                                     createPickupInfo.position) < 10f);
        }

        // returns the pickup index of the nearby scrapper if there is any
        public static ItemIndex NearScrapper(GenericPickupController.CreatePickupInfo createPickupInfo)
        {
            var scrappers = Object.FindObjectsOfType<ScrapperController>();
            try
            {
                var scrapper = scrappers.First(x =>
                    Vector3.Distance(x.transform.position, createPickupInfo.position) < 10f);
                if (scrapper)
                {
                    return scrapper.lastScrappedItemIndex;
                }
            }catch(InvalidOperationException){}

            return ItemIndex.None;
        }

        public static void SubtractOne(uint netid, PickupIndex pickupIndex)
        {
            try
            {
                var ind = Data.allEquipmentIDs.First(i => PickupCatalog.FindPickupIndex(i.Value) == pickupIndex);
                GameManager.items[netid][ind.Key] -= 1;
            }
            catch (InvalidOperationException){}catch(KeyNotFoundException){}

            try
            {
                var ind = Data.allItemIDs.First(i => PickupCatalog.FindPickupIndex(i.Value) == pickupIndex);
                GameManager.items[netid][ind.Key] -= 1;
            }
            catch (InvalidOperationException){}catch(KeyNotFoundException){}
        }
        
        public static uint GetPlayer(CharacterMaster activator)
        {
            var masterController = activator.playerCharacterMasterController;
            var value = masterController.networkUser.netId.Value;
            if (!masterMap.ContainsKey(masterController))
                masterMap[masterController] = value;
            return !GameManager.items.ContainsKey(value) ? masterMap[masterController] : value;
        }
    }
}