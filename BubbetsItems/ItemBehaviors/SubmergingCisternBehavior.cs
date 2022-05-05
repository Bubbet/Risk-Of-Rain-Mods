using BubbetsItems.Items;
using RoR2;
using RoR2.Items;
using UnityEngine;
using UnityEngine.Networking;

namespace BubbetsItems.ItemBehaviors
{
	public class SubmergingCisternBehavior : BaseItemBodyBehavior
	{
		private GameObject? _prefab;
		private GameObject? attachment;
		public GameObject prefab => _prefab ??= BubbetsItemsPlugin.AssetBundle.LoadAsset<GameObject>("SubmersiveCisternNetworkBodyAttachment");

		[ItemDefAssociation(useOnServer = true, useOnClient = false)]
		private static ItemDef? GetItemDef()
		{
			var instance = SharedBase.GetInstance<SubmergingCistern>();
			return instance?.ItemDef;
		}
		
		public void OnDisable()
		{
			if (attachment)
			{
				Destroy(attachment);
				attachment = null;
			}
		}

		private void FixedUpdate()
		{
			if (!NetworkServer.active) return;
			if (stack > 0 != attachment)
			{
				if (stack > 0)
				{
					attachment = Instantiate(prefab);
					attachment.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(body.gameObject);
					return;
				}
				Destroy(attachment);
				attachment = null;
			}
		}
	}
}