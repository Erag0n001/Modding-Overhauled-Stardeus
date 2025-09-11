using Game.UI;
using HarmonyLib;
using ModdingOverhauled.Logging;
using ModdingOverhauled.Utils;

namespace ModdingOverhauled.Misc.Patches;

public static class MainMenuPatches
{
    [HarmonyPatch(typeof(MainMenu), "ShowMainMenu")]
    public static class ShowMainMenuPatch
    {
        [HarmonyPrefix] [HarmonyPriority(Priority.First)]
        public static void Postfix(MainMenu __instance)
        {
            Printer.Warn("Patching main menu");
            MainMenuUtils.Menu = __instance;
        }
    }
}