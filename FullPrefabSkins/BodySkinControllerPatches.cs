using System;
using System.Linq;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace FullPrefabSkins
{
    [HarmonyPatch]
    public static class BodySkinControllerPatches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(SkinDef), "Apply")]
        public static bool SkindefApply(SkinDef __instance, GameObject modelObject)
        {
            var skinModel = __instance.rootObject;
            Debug.Log(modelObject);
            Debug.Log(skinModel);
            var display = modelObject.name.Contains("Display") && modelObject.transform.GetChild(0).GetComponent<ModelSkinDummy>();
            if (modelObject.GetComponent<ModelSkinDummy>() || skinModel.GetComponent<ModelSkinDummy>() || display)
            {
                var characterBody = modelObject.GetComponent<CharacterModel>().body;
                if (characterBody == null || display)
                {
                    __instance.ApplyDisplaySkin(modelObject);
                    return false;
                }
                var modelLocator = characterBody.GetComponent<ModelLocator>();
                __instance.ApplySkin(characterBody, modelLocator);
                return false;
            }
            return true;
        }
    }

    public static class SkinDefExt
    {
        public static void ApplyDisplaySkin(this SkinDef skin, GameObject modelObject)
        {
            Debug.Log("[FullPrefabSkins] Applying Display Skin");
            var skinDef = skin;
            onDisplaySkinApplyBefore?.Invoke(ref skinDef, modelObject);
            var newObj = Object.Instantiate(skinDef.rootObject, modelObject.transform, true);
            onDisplaySkinApplyAfter?.Invoke(modelObject, newObj);
            Object.DestroyImmediate(modelObject.transform.GetChild(0).gameObject);
        }
        public static void ApplySkin(this SkinDef skin, CharacterBody body, ModelLocator modelLocator)
        {
            if (body.GetComponent<SkinAppliedComponent>()) return;
            Debug.Log("[FullPrefabSkins] Applying Skin");
            //Debug.Log("running replace apply from custom component");
            //Debug.Log(skin.nameToken);
            var skinDef = skin;
            onSkinSwap?.Invoke(ref skinDef, body, modelLocator);
            var newModel = skinDef.rootObject.GetComponent<CharacterModel>();
            if (newModel.baseRendererInfos == null)
            {
                newModel.FillRenderInfos();
            }
            newModel.body = body;
            foreach (var x in skinDef.rootObject.GetComponentsInChildren<HurtBox>())
            {
                try
                {
                    x.healthComponent = body.healthComponent;
                    x.gameObject.layer = 12; // EntityPrecise
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }
            var gameObject = modelLocator.modelTransform.gameObject;
            onSkinApplyBefore?.Invoke(newModel.body, newModel, gameObject);
            var newObject = Object.Instantiate(skinDef.rootObject, modelLocator.modelTransform.parent);
            foreach (var x in newObject.GetComponentsInChildren<HurtBox>())
            {
                try
                {
                    x.teamIndex = body.teamComponent.teamIndex;
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }
            onSkinApplyAfter?.Invoke(newModel.body, newObject, gameObject);
            //Object.Destroy(gameObject); //Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(gameObject);
            if (newObject != null)
                modelLocator.modelTransform = newObject.transform; // throws nre on model delete
            body.GetComponents<EntityStateMachine>().FirstOrDefault(x => x.customName == "Body")?.state.OnEnter();
            var posInd = body.GetComponentsInChildren<PositionIndicator>().FirstOrDefault(x => x.name == "PlayerPositionIndicator(Clone)");
            if(posInd != null) posInd.targetTransform = body.coreTransform;
            body.gameObject.AddComponent<SkinAppliedComponent>();
            //var foundInstance = InstanceTracker.GetInstancesList<CharacterModel>().Find(x => x.body == body);
            //Debug.Log(foundInstance);
            //Debug.Log(foundInstance.body);

            //Debug.Log(newModel.baseRendererInfos);
            //newObject.GetComponent<CharacterModel>().baseRendererInfos = newModel.baseRendererInfos;
            /*
        ref var rendererInfos = ref newModel.baseRendererInfos;
        for (int index = 0; index < rendererInfos.Length; ++index)
        {
            ref CharacterModel.RendererInfo local = ref rendererInfos[index];
            if (!(bool) local.renderer)
                Debug.LogErrorFormat("Skin {0} has an empty renderer field in its rendererInfos.", newObject);
            else
            {
                var path = Util.BuildPrefabTransformPath(newModel.transform, local.renderer.transform, false);
                Transform transform2 = newObject.transform.Find(path);
                if (transform2)
                {
                    Renderer component = transform2.GetComponent<Renderer>();
                    if ((bool) component)
                        component.material = local.defaultMaterial;
                }
            }
        }
        */
            //orig in Start
            //field: modelAnimator
            //CharacterDirection 
            //RailMotor
            //field: animator
            //RigidBodyMotor
            //RigidBodyDirection
            //TeleporterInteraction.Awake: modelChildLocator assuming this thing can even have skins
            //CombatHealthBarViewer.GetHealthBarInfo>healthBarInfo.sourceTransform

            //other hard references
            //characterDirection.modelAnimator
            //camera target params as a whole component
            //AimAnimator inputBank and directionComponent
            //--FootstepHandler body and bodyInventory-- // probably not to worry about
            //TalismanAnimator.start
            //teslacoilanimator.start
            //warcryoncombatdisplaycontroller.start
        }
    
        //public static Action<SkinDef, BodyModelSkinController, ModelLocator> onSkinSwap;
        public static Action<CharacterBody, CharacterModel, GameObject> onSkinApplyBefore;
        public static Action<CharacterBody, GameObject, GameObject> onSkinApplyAfter;
        public delegate void ActionRef<T, T1, T2>(ref T arg1, T1 arg2, T2 arg3);
        public delegate void ActionRef<T, T1>(ref T arg1, T1 arg2);
        public static ActionRef<SkinDef, CharacterBody, ModelLocator> onSkinSwap;
        public static ActionRef<SkinDef, GameObject> onDisplaySkinApplyBefore;
        public static Action<GameObject, GameObject> onDisplaySkinApplyAfter;


        public static void FillRenderInfos(this CharacterModel model)
        {
            var renderers = model.GetComponentsInChildren<MeshRenderer>();
            model.baseRendererInfos = renderers.Select(x => new CharacterModel.RendererInfo {
                renderer = x,
                defaultMaterial = x.material,
                defaultShadowCastingMode = ShadowCastingMode.Off,
                ignoreOverlays = false
            }).ToArray();
        }
    }
}