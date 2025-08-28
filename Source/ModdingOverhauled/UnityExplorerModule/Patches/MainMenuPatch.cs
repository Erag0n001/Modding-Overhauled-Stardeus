using Game.UI;
using HarmonyLib;

namespace ModdingOverhauled.UnityExplorerModule.Patches
{
    public static class MainMenuPatch
    {
        [HarmonyPatch(typeof(MainMenu), "ShowMainMenu")]
        public static class Patch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                if(Main.Config.IsUnityExplorerLoaded)
                    UnityExplorerManager.ToggleEditor(true);
            }
        }
    }
}