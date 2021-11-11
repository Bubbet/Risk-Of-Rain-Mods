using System;
using System.Security;
using System.Security.Permissions;
using HarmonyLib;
using RoR2.UI;
using UnityEngine;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
namespace StartingItemsGuiPatch
{
    [HarmonyPatch]
    public class NumberKeyPickerChoiceBehaviorPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(PickupPickerPanel), nameof(PickupPickerPanel.Awake))]
        public static void Postfix(PickupPickerPanel __instance)
        {
            if (!StartingItemsGuiPatch.CommandNumberKeyChoice.Value) return;
            __instance.gameObject.AddComponent<NumberKeyPickerChoiceBehavior>().panel = __instance;
        }
    }
    
    public class NumberKeyPickerChoiceBehavior : MonoBehaviour
    {
        public PickupPickerPanel panel;

        public void Update()
        {
            //var master = panel.pickerController.networkUIPromptController.currentParticipantMaster.playerCharacterMasterController;
            //if (master == null) return;
            //Player player;
            //PlayerCharacterMasterController.CanSendBodyInput(master.networkUser, out _, out player, out _);
            for (var i = 0; i < 9; i++)
            {
                //if (player.GetButtonDown(49 + i))
                if (Input.GetKeyDown((KeyCode) (49 + i)))
                {
                    //Debug.Log("Pressing Key: " + i);
                    try {
                        panel.pickerController.SubmitChoice(i);
                    } catch (IndexOutOfRangeException) {}
                }
            }
        }
    }
}