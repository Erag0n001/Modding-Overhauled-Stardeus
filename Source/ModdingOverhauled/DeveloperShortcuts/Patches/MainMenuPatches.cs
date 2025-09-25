using Game;
using Game.UI;
using Game.Utils;
using HarmonyLib;
using KL.Utils;
using ModdingOverhauled.Misc;
using ModdingOverhauled.Utils;
using Translations = ModdingOverhauled.ConfigModule.Translations;

namespace ModdingOverhauled.DeveloperShortcuts.Patches;

public static class MainMenuPatches
{
    [HarmonyPatch(typeof(MainMenu), "ShowMainMenu")]
    public static class ShowMainMenuPatch
    {
        internal static MainMenuButton Button;
        [HarmonyPrefix]
        public static void Prefix(MainMenu __instance)
        {
            if (Main.Config.DevShortcuts)
            {
                AddButton(__instance);
            }
        }

        public static void AddButton(MainMenu menu)
        {
            Button = MainMenuUtils.CreateButton(Translations.DevQuickTestButton, StartQuickTest);
            MainMenuUtils.AddMainMenuButton(Button, 4);
        }
        
        private static void StartQuickTest(MainMenuButton mainMenuButton)
        {
            DeveloperShortcutsConst.IsDoingQuickest = true;
            The.BB.Set(-2132387920, 0);
            The.BB.Set(-1255412797, "new_game");
            The.BB.Set(362542026, "sandbox");
            Ready.SetGame(status: false, "NewGamePanel");
            var type = AccessTools.TypeByName("Game.UI.UILinks");
            var method = AccessTools.Method(type, "Reset");
            method.Invoke(null, null);
            MiscUtils.LoadSceneTimed("Empty");
            MiscUtils.LoadSceneTimed("Game");
            SaveLoadUtils.ClearCache();
        }
    }
}