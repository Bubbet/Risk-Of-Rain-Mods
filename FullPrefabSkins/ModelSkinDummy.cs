using RoR2;
using UnityEngine;

//[RequireComponent(typeof(CharacterModel))]
namespace FullPrefabSkins
{
    [DisallowMultipleComponent]
    public class ModelSkinDummy : MonoBehaviour
    {
        public void Start()
        {
            //var body = GetComponent<CharacterModel>().body;
            //SkinCatalog.GetBodySkinDef(body.bodyIndex, (int) body.skinIndex).ApplySkin(body, body.GetComponent<ModelLocator>());
        }
    }
}