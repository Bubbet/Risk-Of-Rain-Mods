using RoR2;
using UnityEngine;

namespace BubbetsItems.Behaviours
{
	public class AwakeHighlightRenderAssign : MonoBehaviour
	{
		public void Awake()
		{
			GetComponent<Highlight>().targetRenderer = GetComponentInChildren<Renderer>();
		}
	}
}