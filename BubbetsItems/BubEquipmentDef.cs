using RoR2;
using UnityEngine;

namespace BubbetsItems
{
	[CreateAssetMenu(menuName = "BubbetsItems/BubEquipmentDef")]
	public class BubEquipmentDef : EquipmentDef
	{
		public GameObject displayModelPrefab;
		
		[ContextMenu("Bub Auto Populate Tokens")]
		public new void AutoPopulateTokens()
		{
			string arg = name.ToUpperInvariant().Substring("ItemDef".Length);
			nameToken = $"BUB_{arg}_NAME";
			pickupToken = $"BUB_{arg}_PICKUP";
			descriptionToken = $"BUB_{arg}_DESC";
			loreToken = $"BUB_{arg}_LORE";
		}
	}
}