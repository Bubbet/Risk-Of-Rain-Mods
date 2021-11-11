using System.Linq;
using DropItems;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace KookehsDropItemPatch
{
    [HarmonyPatch(typeof(DropItemHandler), "DropItem")]
    public class KookehsDropItemPatchPatch
    {
        static bool Prefix(Transform charTransform, Inventory inventory, PickupIndex pickupIndex)
        {
            var item = PickupCatalog.GetPickupDef(pickupIndex);
            if (item != null)
            {
                if (KookehsDropItemPatchPlugin._blacklistedItems.Contains(ItemCatalog.GetItemDef(item.itemIndex)))
                {
                    KookehsDropItemPatchPlugin.Log.LogInfo("Failed to drop, blacklisted.");
                    return false;
                }

                //KookehsDropItemPatchPlugin.Log.LogInfo("Length Of Colliders" + colliders.Length);
                if (KookehsDropItemPatchPlugin._needScrapperToDrop.Value)
                {
                    var scrappers = Object.FindObjectsOfType<ScrapperController>(); 
                    // var scrappers = colliders.Where(x => x.gameObject.GetComponent<ScrapperController>() != null);
                    KookehsDropItemPatchPlugin.Log.LogInfo("Length Of Scrapprs" + scrappers.Count());
                    if (!scrappers.Any())
                    {
                        KookehsDropItemPatchPlugin.Log.LogInfo("Failed to drop, no scrappers.");
                        return false;
                    }

                    var nearest = scrappers.Min(x => Vector3.Distance(charTransform.position, x.gameObject.transform.position));
                    if (nearest > KookehsDropItemPatchPlugin._needScrapperToRadius.Value)
                    {
                        KookehsDropItemPatchPlugin.Log.LogInfo("Failed to drop, too far away scrappers.");
                        return false;
                    }
                }
                if (KookehsDropItemPatchPlugin._dropLunarNearPlayers.Value && item.isLunar)
                {
                    Collider[] colliders = Physics.OverlapSphere(charTransform.position, KookehsDropItemPatchPlugin._needScrapperToRadius.Value,
                        LayerIndex.defaultLayer.mask | LayerIndex.fakeActor.mask, QueryTriggerInteraction.UseGlobal);
                    var players = colliders.Where(x =>
                    {
                        var isPlayerControlled = x.gameObject.GetComponent<CharacterBody>()?.isPlayerControlled;
                        return x.transform != charTransform && isPlayerControlled != null && (bool) isPlayerControlled ;
                    });
                    if (!players.Any())
                    {
                        KookehsDropItemPatchPlugin.Log.LogInfo("Failed to drop, no players.");
                        return false;
                    }
                }
            }

            return true;
        }
    }
}