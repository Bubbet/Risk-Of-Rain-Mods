using System.Linq;
using RoR2;
using UnityEngine;

namespace BubbetsItems.Behaviours
{
	public class BubEventFunctions : MonoBehaviour
	{
		public void InteractorOutOfBounds(Interactor interactor)
		{
			var body = interactor.GetComponent<CharacterBody>();
			if (!body) return;
			var zone = InstanceTracker.GetInstancesList<MapZone>().First();
			zone.TeleportBody(body);
		}
	}
}