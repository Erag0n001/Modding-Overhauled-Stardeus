using Game.Scenarios;
using HarmonyLib;
using ModdingOverhauled.Extensions;

namespace ModdingOverhauled.DeveloperShortcuts.Patches;

public static class ScenarioSetupWidgetPatches
{
    [HarmonyPatch(typeof(ScenarioSetupWidget), "Update")]
    public static class StartPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ScenarioSetupWidget __instance)
        {
            if (!Main.Config.DevShortcuts || !DeveloperShortcutsConst.IsDoingQuickest)
            {
                return;
            }
            
            __instance.ChangeStorygen(null, 1);
            __instance.ChangeDifficulty(null, 4);
            __instance.ChangeCommitment(null, 2);
            __instance.DoNext();
        }
    }
}