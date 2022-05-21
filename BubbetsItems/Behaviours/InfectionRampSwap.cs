using System;
using UnityEngine;

namespace BubbetsItems.Behaviours
{
	[ExecuteAlways]
	public class InfectionRampSwap : MonoBehaviour
	{
		public Texture2D ramp;
		public Color color;

		public void ReplaceRamp()
		{
			//child.GetComponent<Renderer>().material.SetTexture("_RemapTex", ramp);
			GetComponentInChildren<Renderer>().material.SetColor("_Color", new Color(3f, 0f, 1f, 1f));
		}
	}
}