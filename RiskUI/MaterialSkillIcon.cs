using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialHud
{
	public class MaterialSkillIcon : MonoBehaviour
	{
		public Image onCooldown;
		public SkillIcon icon;
		public Image mask;
		public TextMeshProUGUI stockText;

		private void Update()
		{
			if (!icon.targetSkill) return;
			var cooldownRemaining = icon.targetSkill.cooldownRemaining;
			var totalCooldown = icon.targetSkill.CalculateFinalRechargeInterval();
			var isCooldown = totalCooldown >= float.Epsilon;
			mask.fillAmount = isCooldown ? cooldownRemaining / totalCooldown : 0;

			onCooldown.enabled = isCooldown && icon.targetSkill.stock <= 0 && mask.fillAmount > float.Epsilon;

			if (icon.targetSkill.maxStock > 1)
			{
				stockText.transform.parent.gameObject.SetActive(true);
			}
			else
			{
				stockText.transform.parent.gameObject.SetActive(false);
			}
		}
	}
}