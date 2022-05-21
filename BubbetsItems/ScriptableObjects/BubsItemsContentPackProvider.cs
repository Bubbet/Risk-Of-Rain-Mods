using System;
using RoR2;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using UnityEngine;

namespace BubbetsItems
{
	
	[CreateAssetMenu(menuName = "BubbetsItems/ContentPackProvider")]
	public class BubsItemsContentPackProvider : SerializableContentPack
	{
		public ExpansionDef[] expansionDefs = {};
		public ItemRelationshipType[] itemRelationshipTypes = {};
		public ItemRelationshipProvider[] itemRelationshipProviders = {};
		public ItemTierDef[] itemTierDefs = {};

		public override ContentPack CreateContentPack()
		{
			var content = base.CreateContentPack();
			content.expansionDefs.Add(expansionDefs);
			content.itemRelationshipTypes.Add(itemRelationshipTypes);
			content.itemRelationshipProviders.Add(itemRelationshipProviders);
			content.itemTierDefs.Add(itemTierDefs);
			return content;
		}
	}
}