using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonoMod.Cil;
using Phedg1Studios.StartingItemsGUI;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;
using StartingItemsGUI = Phedg1Studios.StartingItemsGUI.StartingItemsGUI;

namespace StartingItemsGuiPatch
{
    /*[HarmonyPatch]
    public static class ItemsOverTime
    {
        [HarmonyILManipulator, HarmonyPatch(typeof(GameManager), "SetCharacterMaster")]
        private static void DontSpawnItemsOnStart(ILContext il)
        {
            if (!StartingItemsGuiPatch.ItemsOverTime.Value) return;
            ILCursor c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCall<GameManager>("SpawnItems")
            );
            c.Remove();
            c.Remove();
        }

        [HarmonyILManipulator, HarmonyPatch(typeof(GameManager), "AttemptSpawnItems")]
        private static void DontSpawnItemsOnStartAttempt(ILContext il)
        {
            if (!StartingItemsGuiPatch.ItemsOverTime.Value) return;
            ILCursor c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<SpawnItems>("_connectionID"),
                x => x.MatchCall<GameManager>("SpawnItems")
            );
            c.Remove();
            c.Remove();
            c.Remove();
        }
        
        [HarmonyPrefix, HarmonyPatch(typeof(PickupPickerController), "OnInteractionBegin")]
        public static void InteractionBeginDivert(Interactor activator, PickupPickerController __instance, ref PickupPickerController.Option[] ___options)
        {
            if (!StartingItemsGuiPatch.ItemsOverTime.Value) return;
            if (RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Command)) return;
            SetOptions(activator, __instance, ___options);
        }

        public static void SetOptions(Interactor activator, PickupPickerController pickupPickerController,
            PickupPickerController.Option[] options)
        {
            List<PickupPickerController.Option> newOptions = new List<PickupPickerController.Option>();
            try
            {
                newOptions.Add(
                    options[0]
                );
            }
            catch (IndexOutOfRangeException)
            {
                return; // Probably a scrapper
            }


            CharacterMaster characterMaster;
            CharacterBody component = activator.GetComponent<CharacterBody>();
            characterMaster = component != null ? component.master : null;
            CharacterMaster participantMaster = characterMaster;

            uint netid = PickupStartItemHelper.GetPlayer(participantMaster);

            ItemDef def = null;
            EquipmentDef eqdef = null;
            if (StartingItemsGuiPatch.ItemsOverTimeMixTier.Value)
            {
                var pic = PickupCatalog.GetPickupDef(options[0].pickupIndex);
                if (pic.itemIndex != ItemIndex.None) def = ItemCatalog.GetItemDef(pic.itemIndex);
                if (pic.equipmentIndex != EquipmentIndex.None)
                    eqdef = EquipmentCatalog.GetEquipmentDef(pic.equipmentIndex);
            }

            foreach (var tup in GameManager.items[netid])
            {
                var item = tup.Key;
                var amount = tup.Value;
                if (amount <= 0) continue;

                if (StartingItemsGuiPatch.ItemsOverTimeMixTier.Value)
                {
                    if (def != null)
                    {
                        if (!PickupStartItemHelper.MatchItem(def, item)) continue;
                    }

                    if (eqdef != null)
                    {
                        if (!PickupStartItemHelper.MatchItem(eqdef, item)) continue;
                    }
                }

                newOptions.Add(new PickupPickerController.Option
                {
                    available = true,
                    pickupIndex = PickupStartItemHelper.FindPickupIndex(item)
                });
            }

            pickupPickerController.SetOptionsServer(newOptions.ToArray());
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PickupPickerController), "HandlePickupSelected")]
        static void HandlePickupSelected(int choiceIndex, ref PickupPickerController.Option[] ___options, NetworkUIPromptController ___networkUIPromptController)
        {
            if (!StartingItemsGuiPatch.ItemsOverTime.Value) return;
            if (!NetworkServer.active) return;
            if (RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Command)) return;
            //Debug.Log("choice: " + choiceIndex + " count: " + ___options.Count());
            ref var ptr = ref ___options[choiceIndex];
            if (!ptr.available) return;
            if (choiceIndex <= 0) return;


            uint netid = PickupStartItemHelper.GetPlayer(___networkUIPromptController.currentParticipantMaster);
            
            PickupStartItemHelper.SubtractOne(netid, ptr.pickupIndex);
        }
    }//*/

    [HarmonyPatch]
    public static class ItemsOverTime
    {
        [HarmonyPostfix, HarmonyPatch(typeof(Data), nameof(Data.RefreshInfo))]
        public static void DataPostfix()
        {
            Data.modEnabled = true;
        }

        /*
        [HarmonyPrefix, HarmonyPatch(typeof(Phedg1Studios.StartingItemsGUI.StartingItemsGUI), "OnRunStartGlobal")]
        public static void StartPrefix()
        {
            StartingItemsGuiPatch.LocalItems.Clear();
        }
        [HarmonyPostfix, HarmonyPatch(typeof(GameManager), nameof(GameManager.SendItems))]
        public static void StoreItems(NetworkUser networkUser)
        {
            if (networkUser.isLocalPlayer)
                StartingItemsGuiPatch.LocalItems.Add(networkUser.netId.Value, GameManager.GetItemsPurchased());
        }*/

        [HarmonyPostfix, HarmonyPatch(typeof(NetworkUser), "OnEnable")]
        public static void AddUser(NetworkUser __instance)
        {
            
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Data), nameof(Data.RefreshInfo))]
        public static void EnableMod()
        {
            //var givenItem = new ItemPurchased();
            //!GameManager.status[givenItem._connectionID][0]
            //!GameManager.items[givenItem._connectionID].ContainsKey(givenItem._itemID)
            //Data.ItemExists(givenItem._itemID)
            
            //status 0 is used in attempt spawn items, which allows it to spawn items if false
            //status gets cleared in clearitems, which gets called when you return to title. and in pregamecontrolleronenable
            //status 0 is checked for false when receivingitem, but never set to true here
            //status 1 is set to true when a character master has been assigned to the netid
            //status 0 must be true for below
            //status 1 must be true for below
            //status 2 is set to true after evaluating the above in spawn items
            //status 0 gets set to true when overrideclientitems is true in that function
            
                
            Data.modEnabled = true;
        }
        
        [HarmonyPrefix, HarmonyPatch(typeof(GameManager), nameof(GameManager.SpawnItems))]
        public static bool DisableSpawnItems()
        {
            return !StartingItemsGuiPatch.ItemsOverTime.Value;
        }
        
        [HarmonyPrefix, HarmonyPatch(typeof(PickupPickerController), "OnInteractionBegin")]
        public static void InteractionBeginDivert(Interactor activator, PickupPickerController __instance, ref PickupPickerController.Option[] ___options)
        {
            if (!StartingItemsGuiPatch.ItemsOverTime.Value) return;
            if (RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Command)) return;
            if (___options.Length <= 0) return; // Probably a scrapper
            if (!__instance.gameObject.name.Contains("Command")) return;

            try
            {
                var master = activator.GetComponent<CharacterBody>().master;
                var who = master.playerCharacterMasterController.networkUser.netId.Value;

                var items = GameManager.items[who]; //StartingItemsGuiPatch.LocalItems[who]; //GameManager.items[who];
                var first = ___options[0];
                ___options = new[] {first}.Union(items.Where(x => x.Value > 0 && SameTier(GetItem(x.Key), first.pickupIndex)).Select(x =>
                    new PickupPickerController.Option
                        {available = true, pickupIndex = GetItem(x.Key)})).ToArray();
                __instance.SetOptionsServer(___options);
                if (___options.Length == 1)
                {
                    __instance.SubmitChoice(0);
                }
            }
            catch (Exception e)
            {
                StartingItemsGuiPatch.log.LogError(e);
                __instance.SubmitChoice(0);
            }
        }

        private static bool SameTier(PickupIndex getItem, PickupIndex firstPickupIndex)
        {   
            var command = PickupCatalog.GetPickupDef(firstPickupIndex);
            var start = PickupCatalog.GetPickupDef(getItem);
            if (start == null || command == null) return false;
            if (command.equipmentIndex == RoR2Content.Equipment.QuestVolatileBattery.equipmentIndex) return true;
            if (command.itemIndex != ItemIndex.None && start.itemIndex != ItemIndex.None)
            {
                return ItemCatalog.GetItemDef(command.itemIndex).tier == ItemCatalog.GetItemDef(start.itemIndex).tier;
            }
            return command.equipmentIndex != EquipmentIndex.None && start.equipmentIndex != EquipmentIndex.None;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PickupPickerController), "HandlePickupSelected")]
        static void HandlePickupSelected(int choiceIndex, PickupPickerController __instance, ref PickupPickerController.Option[] ___options, NetworkUIPromptController ___networkUIPromptController)
        {
            if (!StartingItemsGuiPatch.ItemsOverTime.Value) return;
            if (!NetworkServer.active) return;
            if (RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Command)) return;
            
            if (choiceIndex <= 0) return;
            if (!__instance.gameObject.name.Contains("Command")) return;

            try
            {
                var pickup = GetItem(___options[choiceIndex].pickupIndex);
                var who = ___networkUIPromptController.currentParticipantMaster.playerCharacterMasterController
                    .networkUser.netId.Value;
                GameManager.items[who][pickup] -= 1;
                /*
                if(NetworkClient.active)
                    StartingItemsGuiPatch.LocalItems[who][pickup] -= 1;*/

                DoRemainingMessage();
            }
            catch (Exception e)
            {
                StartingItemsGuiPatch.log.LogError(e);
            }
        }

        public static Dictionary<ItemTier, NetworkUser> oldUnique = new Dictionary<ItemTier, NetworkUser>();
        private static NetworkUser oldEqUnique;

        private static void DoRemainingMessage()
        {
            /*
            var temp = new Dictionary<NetworkUser, Dictionary<ItemTier, int>>();
            // for every item tier/equipment check if only one person has items left
            foreach (var pair in GameManager.items)
            {
                var itemAmounts = new Dictionary<ItemTier, int>();
                var eqAmount = 0;
                var who = NetworkUser.readOnlyInstancesList.FirstOrDefault(x => pair.Key == x.netId.Value);
                if(who == default) continue;
                //Chat.AddMessage($"{who.userName} is the only player with {tier} items left."); // who.userName;
                foreach (var key in itemAmounts.Keys)
                {
                    itemAmounts[key] = GetTotalItemCountOfTier(pair.Key, key);
                }

                //equipment = GetTotalEquipmentCount(pair.Key)}; todo after
                temp[who] = itemAmounts;
            }*/

            var totals = new Dictionary<ItemTier, List<NetworkUser>>()
            {
                [ItemTier.Tier1] = new List<NetworkUser>(),
                [ItemTier.Tier2] = new List<NetworkUser>(),
                [ItemTier.Tier3] = new List<NetworkUser>(),
                [ItemTier.Boss] = new List<NetworkUser>(),
                [ItemTier.Lunar] = new List<NetworkUser>()
            };
            var eqtotal = new List<NetworkUser>();
            
            foreach (var pair in GameManager.items)
            {
                var who = NetworkUser.readOnlyInstancesList.FirstOrDefault(x => pair.Key == x.netId.Value);
                if (who == default) continue;
                if (!who.isParticipating || who.master.IsDeadAndOutOfLivesServer()) continue;

                foreach (var totalpair in totals)
                {
                    var amount = GetTotalItemCountOfTier(pair.Key, totalpair.Key);
                    if (amount > 0) totalpair.Value.Add(who);
                }

                if (GetTotalEquipmentCount(pair.Key) > 0)
                {
                    eqtotal.Add(who);
                }
            }

            var unique = new Dictionary<ItemTier, NetworkUser>();
            NetworkUser equnique = null;

            foreach (var pair in totals)
            {
                //Debug.Log(pair.Key + ": " + pair.Value.Count);
                if (pair.Value.Count == 1)
                    unique[pair.Key] = pair.Value[0];
            }

            if (eqtotal.Count == 1)
                equnique = eqtotal[0];

            foreach (var pair in unique)
            {
                if(!oldUnique.ContainsKey(pair.Key) && pair.Value != null) // TODO replace this with old != new
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage {baseToken = $"{pair.Value.userName} is the only player with {pair.Key} items left."}); // TODO do translation keys for this
            }
            if(oldEqUnique == null && equnique != null)
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage {baseToken = $"{equnique.userName} is the only player with Equipment left."}); // TODO do translation keys for this
            
            oldUnique = unique;
            oldEqUnique = equnique;

            /*
             * for every item tier get the unique user if any
             *
             * for every item tier get if they have any items in tier
             * add them to the list if they have the tier
             *
             * check the list of tiers, if the list.count == total-1 and you are not in the list then you are the unique person for that tier
             */
        }


        public static int GetTotalItemCountOfTier(uint who, ItemTier itemTier)
        {
            var amount = 0;
            foreach (var pair in GameManager.items[who])
            {
                var def = PickupCatalog.GetPickupDef(GetItem(pair.Key));
                if (def != null && def.itemIndex != ItemIndex.None)
                {
                    if (ItemCatalog.GetItemDef(def.itemIndex).tier == itemTier)
                    {
                        amount += Math.Max(0, pair.Value);
                    }
                }
            }
            return amount;
        }
        public static int GetTotalEquipmentCount(uint who)
        {
            var amount = 0;
            foreach (var pair in GameManager.items[who])
            {
                var def = PickupCatalog.GetPickupDef(GetItem(pair.Key));
                if (def != null && def.equipmentIndex != EquipmentIndex.None)
                {
                    amount += Math.Max(0, pair.Value);
                }
            }
            return amount;
        }

        public static PickupIndex GetItem(int num)
        {
            if (Data.allItemIDs.ContainsKey(num))
            {
                return PickupCatalog.FindPickupIndex(Data.allItemIDs[num]);
            }

            if (Data.allEquipmentIDs.ContainsKey(num))
            {
                return PickupCatalog.FindPickupIndex(Data.allEquipmentIDs[num]);
            }
            
            return PickupIndex.none;
        }

        public static int GetItem(PickupIndex pickupIndex)
        {
            var def = PickupCatalog.GetPickupDef(pickupIndex);
            if (def == null) return -1;
            
            if (Data.allItemIDs.ContainsValue(def.itemIndex))
            {
                foreach (var itemID in Data.allItemIDs.Where(itemID => itemID.Value == def.itemIndex))
                {
                    return itemID.Key;
                }
            }

            if (Data.allEquipmentIDs.ContainsValue(def.equipmentIndex))
            {
                foreach (var itemID in Data.allEquipmentIDs.Where(itemID => itemID.Value == def.equipmentIndex))
                {
                    return itemID.Key;
                }
            }

            return -1;
        }
    }
}