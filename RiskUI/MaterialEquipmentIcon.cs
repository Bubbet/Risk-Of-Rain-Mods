using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialHud
{
	public class MaterialEquipmentIcon : MonoBehaviour
	{
		public EquipmentIcon icon;
		public Image onCooldown;
		public Image mask;
		public TextMeshProUGUI stockText;
		public GameObject keyText;

		public void Update()
		{
			if (!icon.hasEquipment)
			{
				stockText.transform.parent.gameObject.SetActive(false);
				mask.fillAmount = 0;
				onCooldown.enabled = false;
				return;
			}
			
			var equipmentState = icon.displayAlternateEquipment ? icon.targetInventory.alternateEquipmentState : icon.targetInventory.currentEquipmentState;
			
			var now = Run.FixedTimeStamp.now;
			var chargeFinishTime = equipmentState.chargeFinishTime;
			
			onCooldown.enabled = icon.currentDisplayData.showCooldown;
			var cooldown = Mathf.Max(0, chargeFinishTime - now);
			if (float.IsPositiveInfinity(cooldown)) cooldown = 0;
			var totalCooldown = equipmentState.equipmentDef.cooldown;
			if (true)
				totalCooldown *= icon.targetInventory.CalculateEquipmentCooldownScale();
			mask.fillAmount = cooldown / totalCooldown;

			stockText.transform.parent.gameObject.SetActive(icon.currentDisplayData.maxStock > 1);
		}
	}
}