using System;
using RoR2;
using UnityEngine;

namespace StartingItemsGuiPatch
{
    public class CommandNameChangeBehaviour : MonoBehaviour
    {
        public void Start()
        {
            var options = GetComponent<PickupPickerController>().options;
            if (options.Length == 0) return;
            var pickupDef = PickupCatalog.GetPickupDef(options[0].pickupIndex);
            var displayname = GetComponent<GenericDisplayNameProvider>();
            displayname.SetDisplayToken(Language.GetString(displayname.GetDisplayName()) + " (" + Language.GetString(pickupDef.nameToken) + ")");
        }
    }
}