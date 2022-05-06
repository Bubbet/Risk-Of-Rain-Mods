using UnityEngine;

namespace MaterialHud
{
	public class HideIfFirst : MonoBehaviour
	{
		public GameObject target;
		public int position;
		private void Awake()
		{ 
			target.SetActive(transform.GetSiblingIndex() != position);
		}
	}
}