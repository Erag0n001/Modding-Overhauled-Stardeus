using Game.Constants;
using Game.Rendering;
using HarmonyLib;
using ModdingOverhauled.LayerModule.Definitions;
// ReSharper disable UnusedType.Global
// ReSharper disable InconsistentNaming

namespace ModdingOverhauled.LayerModule.Patches;

public static class RenderLayersPatches {
    [HarmonyPatch(typeof(RenderLayers), "BuildDefinitions")]
    public static class BuildDefinitionsPatches {
        public static bool Prefix(ref RenderLayerDef[] __result) {
            __result = LayerDef.RenderLayerDefs;
            return false;
        }
    }
}