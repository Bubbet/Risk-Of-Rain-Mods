using System.Collections.Generic;
using HarmonyLib;
using MonoMod.Cil;
using Phedg1Studios.StartingItemsGUI;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace StartingItemsGuiPatch
{
    [HarmonyPatch]
    public class AllModesPatch
    {
        [HarmonyILManipulator, HarmonyPatch(typeof(DataEarntPersistent), "UpdateUserPointsStages")]
        static void PUpdateUserPointsStages(ILContext il) => StageDel(il);
        
        [HarmonyILManipulator, HarmonyPatch(typeof(DataEarntPersistent), "UpdateUserPointsBoss")]
        static void PUpdateUserPointsBoss(ILContext il) => BossDel(il);
        
        [HarmonyILManipulator, HarmonyPatch(typeof(DataEarntConsumable), "UpdateUserPointsStages")]
        static void CUpdateUserPointsStages(ILContext il) => StageDel(il);
        
        [HarmonyILManipulator, HarmonyPatch(typeof(DataEarntConsumable), "UpdateUserPointsBoss")]
        static void CUpdateUserPointsBoss(ILContext il) => BossDel(il);

        private static void BossDel(ILContext il)
        {
            if (!StartingItemsGuiPatch.AllModes.Value) return;
            var c = new ILCursor(il);
            c.Remove();
            c.Remove();
            c.Remove();
        }

        private static void StageDel(ILContext il)
        {
            if (!StartingItemsGuiPatch.AllModes.Value) return;
            var c = new ILCursor(il);
            c.Remove();
            c.Remove();
            c.Remove();
            c.Remove();
            c.Remove();
            c.GotoNext(
                x => x.MatchMul(),
                x => x.MatchStloc(0),
                x => x.MatchLdsfld<Data>("earningMethod")
            );
            c.Index += 2;
            c.Remove();
            c.Remove();
        }
    }

    [HarmonyPatch]
    public class ShopInLobby
    {
        
        [HarmonyPostfix, HarmonyPatch(typeof(UIDrawer), "SetMenuTitle")]
        private static void SetMenuTitle()
        {
            var si = UIDrawer.startingItems;
            si.onExit.Invoke();
            si.shouldDisplay = false;
            si.gameObject.SetActive(false);
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(CharacterSelectController), "Awake")]
        private static void CharacterSelectController_Awake(CharacterSelectController __instance)
        {
            var footerPanel = __instance.transform.Find("SafeArea/FooterPanel");
            var quitButton = footerPanel.GetChild(0); 
            var shopButton = Object.Instantiate(quitButton, footerPanel.transform);
                
            shopButton.localPosition = quitButton.localPosition + Vector3.left * 207.8608f;
            var shopGo = shopButton.gameObject;
            shopGo.name = "Starting Items Button";
            shopGo.SetActive(true);
                
            Object.Destroy(shopGo.GetComponent<LanguageTextMeshController>());
            shopButton.GetChild(0).GetComponent<HGTextMeshProUGUI>().SetText("Starting Items");

            var shopHgButton = shopGo.GetComponent<HGButton>();
            shopHgButton.onClick = new Button.ButtonClickedEvent();
            shopHgButton.onClick.AddListener(delegate
            {
                if (UIDrawer.rootTransform is null) UIDrawer.CreateCanvas();
                var currentMenuScreen = UIDrawer.startingItems;
                currentMenuScreen.gameObject.SetActive(true);
                currentMenuScreen.shouldDisplay = true;
                currentMenuScreen.onEnter.Invoke();
            });
        }

        private static GameObject ClearAllButton;
        //Add Clear All Button
        [HarmonyPrefix, HarmonyPatch(typeof(UIDrawer), "DrawBlackButtons")]
        public static void MakeClearAllButton(float ___storeHeight)
        {
            if (ClearAllButton) return;
            var button = ButtonCreator.SpawnBlackButton(UIDrawer.rootTransform.gameObject, new Vector2(UIConfig.blackButtonWidth, UIConfig.blackButtonHeight), "Clear All", new List<TextMeshProUGUI>(), true);
            button.transform.parent.GetComponent<RectTransform>().localPosition = new Vector3(UIConfig.offsetHorizontal + (UIConfig.blackButtonWidth + UIConfig.spacingHorizontal) * 2, -UIConfig.offsetVertical - UIConfig.blueButtonHeight - UIConfig.spacingVertical - ___storeHeight - UIConfig.spacingVertical, 0f);
            button.GetComponent<HGButton>().onClick.AddListener(delegate()
            {
                //Data.RightClick(int itemID);
                if (Data.mode == DataEarntConsumable.mode)
                {
                    DataEarntConsumable.itemsPurchased[Data.profile[Data.mode]].Clear();
                }
                else if (Data.mode == DataEarntPersistent.mode)
                {
                    DataEarntPersistent.itemsPurchased[Data.profile[Data.mode]].Clear();
                }
                else if (Data.mode == DataFree.mode)
                {
                    DataFree.itemsPurchased[Data.profile[Data.mode]].Clear();
                }

                Data.MakeDirectoryExist();
                Data.SaveConfigProfile();
                UIDrawer.Refresh();
            });
            ClearAllButton = button;
        }
    }
}