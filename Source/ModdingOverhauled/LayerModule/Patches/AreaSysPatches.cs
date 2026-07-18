using Game.Data;
using Game.Systems;
using Game.Systems.Areas;
using HarmonyLib;
using ModdingOverhauled.LayerModule.Const;
using ModdingOverhauled.LayerModule.Definitions;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModdingOverhauled.LayerModule.Patches;

public static class AreaSysPatches {
    [HarmonyPatch(typeof(AreaSys), nameof(AreaSys.ResolveAreaTypeAt))]
    public static class ResolveAreaAtPatches {
        [HarmonyPrefix]
        public static bool Prefix(AreaSys __instance, int i, ref AreaType __result) {
            var foundFloor = false;
            Tile floor = null;
            foreach (var layer in LayerDef.AffectsPathFindingLayers[LayerConstants.FloorPathfindingIndex]) {
                floor = __instance.S.Grids[layer.LayerId].Get(i);
                if (floor?.IsConstructed == true && floor.Passability?.IsUnbuiltOverrideActive != true) {
                    foundFloor = true;
                    break;
                }
            }
            if (!foundFloor) {
                __result = AreaType.Space;
                return false;
            }

            var foundWall = false;
            foreach (var layer in LayerDef.AffectsPathFindingLayers[LayerConstants.WallPathfindingIndex]) {
                var wall = __instance.S.Grids[layer.LayerId].Get(i);
                if (wall?.IsConstructed == true && wall.Passability?.IsUnbuiltOverrideActive != true) {
                    __result = wall!.Door != null ? AreaType.Door : AreaType.Wall;
                    foundWall = true;
                    break;
                }
            }

            if (foundWall) {
                return false;
            }


            if (!(floor.Integrity.CanHaveFullIntegrity || floor.Passability.IsWalkable)) {
                __result = AreaType.Space;
                return false;
            }
            
            foreach (var layer in LayerDef.AffectsPathFindingLayers[LayerConstants.ObjPathfindingIndex]) {
                var obj = __instance.S.Grids[layer.LayerId].Get(i);
                if (obj != null && obj.Transform.IsChild) {
                    obj = obj.Transform.Parent;
                }
                if (obj != null
                    && obj.IsConstructed
                    && obj.Passability?.IsUnbuiltOverrideActive != true
                    && !obj.Passability.IsPassable) {
                 
                    __result = AreaType.BlockedByObject;
                    return false;
                }
            }

            __result = AreaType.Floor;
            return false;
        }
    }
}