using System;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialHud
{
	public class CloneFillValue : MonoBehaviour
	{
		public Image from;
		private Image _to;

		private void Awake()
		{
			_to = GetComponent<Image>();
		}

		private void Update()
		{
			_to.fillAmount = from.fillAmount;
		}
	}
}