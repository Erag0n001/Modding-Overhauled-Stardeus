using System.Linq;
using Game.Scenarios;
using Game.Systems.Space;
using HarmonyLib;
using KL.Randomness;
using ModdingOverhauled.Extensions;

namespace ModdingOverhauled.DeveloperShortcuts.Patches;

public static class SpaceSetupWidgetPatches
{
    [HarmonyPatch(typeof(SpaceSetupWidget), "Update")]
    public static class NextPatch
    {
        [HarmonyPostfix]
        public static void Postfix(SpaceSetupWidget __instance, ref SpaceRegion ___selectedRegion)
        {
            if (!Main.Config.DevShortcuts)
            {
                return;
            }
            
            if (___selectedRegion == null)
            {
                while (___selectedRegion == null || ! ___selectedRegion.Sectors.Any(x => x.IsHyperjumpRelay))
                {
                    __instance.SpaceMapViz().SelectRandomRegion(Rng.Unseeded, 1f);
                }
            }

            __instance.DoNext();
        }
    }
}