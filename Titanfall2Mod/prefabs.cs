using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace Titanfall2Mod
{
    public static class Prefabs
    {
        // The order of your SurvivorDefs in your SerializableContentPack determines the order of body/displayPrefab variables here.
        // This lets you reference any bodyPrefabs or displayPrefabs throughout your code.

        public static readonly List<GameObject> BodyPrefabs = new List<GameObject>();
        //internal static readonly List<GameObject> DisplayPrefabs = new List<GameObject>();
        //internal static readonly List<GameObject> MasterPrefabs = new List<GameObject>();

        public static CharacterBody pilotBodyPrefab;
        public static CharacterBody titanBodyPrefab;

        private static PhysicMaterial _ragdollMaterial;

        internal static void Init()
        {
            GetPrefabs();
            AddPrefabReferences();
        }

        internal static void AddPrefabReferences()
        {
            ForEachReferences();

            
            //If you want to change the 'defaults' set in ForEachReferences, then set them for individual bodyPrefabs here.
            //This is if you want to use a custom crosshair or other stuff.

            // bodyPrefabs[0].GetComponent<CharacterBody>().crosshairPrefab = ...whatever you wanna set here.
        }

        // Some variables have to be set and reference assets we don't have access to in Thunderkit.
        // So instead we set them here.
        private static void ForEachReferences()
        {
            foreach (var g in BodyPrefabs)
            {
                var cb = g.GetComponent<CharacterBody>();
                cb.crosshairPrefab = Resources.Load<GameObject>("prefabs/crosshair/StandardCrosshair");
                cb.preferredPodPrefab = Resources.Load<GameObject>("prefabs/networkedobjects/SurvivorPod");

                var fs = g.GetComponentInChildren<FootstepHandler>();
                if (fs != null)
                    fs.footstepDustPrefab = Resources.Load<GameObject>("prefabs/GenericFootstepDust");

                SetupRagdoll(g);

                Debug.Log(g.name);
                if (g.name == "PilotBody") pilotBodyPrefab = g.GetComponent<CharacterBody>();
                if (g.name == "Tf2TitanBody") titanBodyPrefab = g.GetComponent<CharacterBody>();
            }
            
            /*
            foreach (var masterPrefab in masterPrefabs)
            {
                
            }*/
        }

        // Code from the original henry to setup Ragdolls for you.
        // This is so you dont have to manually set the layers for each object in the bones list.
        private static void SetupRagdoll(GameObject model)
        {
            var ragdollController = model.GetComponent<RagdollController>();

            if (!ragdollController) return;

            if (_ragdollMaterial == null)
                _ragdollMaterial = Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody")
                    .GetComponentInChildren<RagdollController>().bones[1].GetComponent<Collider>().material;

            foreach (var i in ragdollController.bones)
                if (i)
                {
                    i.gameObject.layer = LayerIndex.ragdoll.intVal;
                    var j = i.GetComponent<Collider>();
                    if (!j) continue;
                    j.material = _ragdollMaterial;
                    j.sharedMaterial = _ragdollMaterial;
                }
        }

        // Find all relevant prefabs within the content pack, per SurvivorDefs.
        private static void GetPrefabs() //wack
        {
            var d = Assets.mainContentPack.bodyPrefabs;
            foreach (var s in d)
            {
                BodyPrefabs.Add(s);
                //DisplayPrefabs.Add(s.displayPrefab);
            }

            //foreach (var ma in Assets.mainContentPack.masterPrefabs) MasterPrefabs.Add(ma);
        }
    }
}