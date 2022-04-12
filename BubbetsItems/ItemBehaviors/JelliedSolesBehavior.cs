using BubbetsItems.Items;
using RoR2;
using RoR2.Items;

namespace BubbetsItems.ItemBehaviors
{
	public class JelliedSolesBehavior : BaseItemBodyBehavior
	{
		[ItemDefAssociation(useOnServer = true, useOnClient = false)]
		private static ItemDef GetItemDef()
		{
			return JelliedSoles.instance.ItemDef;
		}

		public float storedDamage;
	}
}