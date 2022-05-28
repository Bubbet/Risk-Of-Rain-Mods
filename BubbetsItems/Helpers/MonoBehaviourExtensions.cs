using UnityEngine;

namespace BubbetsItems.Helpers
{
	public static class MonoBehaviourExtensions
	{
		public static T EnsureComponent<T>(this MonoBehaviour monoBehaviour) where T : MonoBehaviour
		{
			var comp = monoBehaviour.GetComponent<T>();
			if (!comp)
				comp = monoBehaviour.gameObject.AddComponent<T>();
			return comp;
		}
		
		public static bool EnsureComponent<T>(this MonoBehaviour monoBehaviour, out T comp) where T : MonoBehaviour
		{
			comp = monoBehaviour.GetComponent<T>();
			if (comp) return true;
			
			comp = monoBehaviour.gameObject.AddComponent<T>();
			return false;
		}
	}
}