using System.Linq;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WhatAmILookingAt
{
	public class WailaHud : MonoBehaviour
	{
		private HUD _hud;
		private HGTextMeshProUGUI textMesh;
		private Collider[] sphereResults = new Collider[20];

		private void Awake()
		{
			_hud = GetComponent<HUD>();
			BuildHud();
		}

		private void BuildHud()
		{
			var uiContainer = new GameObject("WailaContainer");
			var inven = _hud.itemInventoryDisplay.transform.parent;
			uiContainer.transform.SetParent(inven.parent);
			uiContainer.transform.SetSiblingIndex(inven.GetSiblingIndex() + 1);
			var rect = uiContainer.AddComponent<RectTransform>();
			var elem = uiContainer.AddComponent<LayoutElement>();

			elem.minHeight = 60;
			elem.preferredWidth = 300;

			rect.pivot = new Vector2(0.5f, 0);
			rect.anchoredPosition = rect.pivot;

			//var image = uiContainer.AddComponent<Image>();
			//image.material = _hud.itemInventoryDisplay.GetComponentInChildren<Image>().material;

			//var textContainer = new GameObject("TextContainer");
			//textContainer.transform.SetParent(uiContainer.transform);
			//var textMesh = textContainer.AddComponent<HGTextMeshProUGUI>();
			textMesh = uiContainer.AddComponent<HGTextMeshProUGUI>();
			textMesh.fontSize = 16;
			textMesh.alignment = TextAlignmentOptions.Center;
			textMesh.text = "";
		}

		private void FixedUpdate()
		{
			/*
			var controller = _hud.cameraRigController;
			var context = controller.cameraModeContext;
			var mod = controller.cameraMode;
			//var instanceData =  (CameraModePlayerBasic.InstanceData) mod.camToRawInstanceData[context.cameraInfo.cameraRigController];
			var mode = (CameraModePlayerBasic) mod;
			Ray crosshairRaycastRay = mode.GetCrosshairRaycastRay(context, Vector2.zero, mode.CalculateTargetPivotPosition(context), controller.currentCameraState);
			RaycastHit[] array = Physics.RaycastAll(crosshairRaycastRay, context.cameraInfo.cameraRigController.maxAimRaycastDistance, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore);
			if (!array.Any() && textMesh.text != "")
			{
				textMesh.text = "";
				return;
			}

			var hit = array[0];
			textMesh.text = hit.collider.gameObject.name;
			*/
			var controller = _hud.cameraRigController;
			var body = controller.targetBody;

			var keyPressCondition =
				WhatAmILookingAtPlugin.RequireTABForInWorld!.Value ==
				WhatAmILookingAtPlugin.InWorldOptions.WhileScoreboardOpen && _hud.localUserViewer?.inputPlayer != null && _hud.localUserViewer.inputPlayer.GetButton("info");

			if (!keyPressCondition && WhatAmILookingAtPlugin.RequireTABForInWorld.Value != WhatAmILookingAtPlugin.InWorldOptions.AlwaysOn || !body || !GetInfo(body, out var localizedName, out var gObject))
			{
				textMesh.text = "";
				return;
			}

			var str = "";
			var sstr = "";
			WailaInWorldChecks.PickupCheck(gObject, ref sstr); // I dont like this
			if (sstr != "")
			{
				WailaInWorldChecks.InteractableCheck(gObject, ref str);
				if (str != "")
				{
					var start = WhatAmILookingAtPlugin.GetModString(str);
					textMesh.text = localizedName + "\n" + start!.Substring(0, start.Length - 8) + " (" +
					                WhatAmILookingAtPlugin.GetModString(sstr) + ")</color>"; // i hate this too
					return;
				}
			}

			str = sstr != "" ? sstr : WailaInWorldChecks.GetIdentifier(gObject);
			textMesh.text = localizedName + "\n" + WhatAmILookingAtPlugin.GetModString(str);
		}
		
		

		private bool GetInfo(CharacterBody characterBody, out string localizedName, out GameObject gObject)
		{
			var aimRay = characterBody.inputBank.GetAimRay();
			
			#region from PingIndicator
			if (PingerController.GeneratePingInfo(aimRay, characterBody.gameObject, out var pingInfo))
			{
				if (pingInfo.targetGameObject)
				{
					gObject = pingInfo.targetGameObject;
					localizedName = GetInfoFromObject(gObject);
					return true;
				}
			}
			#endregion

			#region from InteractionDriver
			var driver = characterBody.GetComponent<InteractionDriver>();
			var obj = driver.interactor.FindBestInteractableObject(aimRay, 100f, aimRay.origin, 1f);
			if (obj)
			{
				gObject = obj;
				localizedName = GetInfoFromObject(gObject);
				return true;
			}
			#endregion

			#region Regular Raycast
			var mask = LayerIndex.enemyBody.mask | LayerIndex.ragdoll.mask | LayerIndex.entityPrecise.mask | LayerIndex.defaultLayer.mask | LayerIndex.world.mask;
			
			if (Physics.Raycast(aimRay, out var raycastHit, 1000f, mask, QueryTriggerInteraction.Collide))
			{
				var entity = EntityLocator.GetEntity(raycastHit.collider.gameObject);
				if (!entity)
				{
					entity = raycastHit.collider.gameObject;
				}

				var hurtBox = entity.GetComponent<HurtBox>();
				if (hurtBox && hurtBox.healthComponent)
				{
					gObject = hurtBox.healthComponent.body.gameObject;
					localizedName = GetInfoFromObject(gObject);
					return true;
				}

				if(entity.IsInScene())
				{
					// This is a nasty solution

					var scene = SceneManager.GetActiveScene();
					var def = SceneCatalog.GetSceneDef(SceneCatalog.FindSceneIndex(scene.name));
					if (def)
					{
						gObject = entity;
						localizedName = Language.GetString(def.nameToken);// + " (" + entity.name + ")";
						return true;
					}
				}

				localizedName = entity.name;
				gObject = entity;
				return true;
			}

			/*
			Physics.OverlapSphereNonAlloc(aimRay.origin, 1f, sphereResults, mask, QueryTriggerInteraction.Collide);
			if (sphereResults.Any()){
				gObject = sphereResults[0].gameObject;
				localizedName = gObject.name;
				return true;
			}*/
			#endregion

			localizedName = null;
			gObject = null;
			return false;
		}

		private string GetInfoFromObject(GameObject gObject)
		{
			var bestBodyName = Util.GetBestBodyName(gObject);

			var shopTerminal = gObject.GetComponent<ShopTerminalBehavior>();
			if (shopTerminal)
			{
				
				//CostTypeCatalog.GetCostTypeDef(pingTargetPurchaseInteraction.costType).BuildCostStringStyled(pingTargetPurchaseInteraction.cost, PingIndicator.sharedStringBuilder, false, true);
				if (!shopTerminal.pickupIndexIsHidden && shopTerminal.pickupDisplay)
				{

					var def = PickupCatalog.GetPickupDef(shopTerminal.CurrentPickupIndex());
					return bestBodyName + " (" + Language.GetString(def?.nameToken ?? PickupCatalog.invalidPickupToken) + ")";
				}
			}

			return bestBodyName == "???" ? gObject.name : bestBodyName;
		}
	}
}