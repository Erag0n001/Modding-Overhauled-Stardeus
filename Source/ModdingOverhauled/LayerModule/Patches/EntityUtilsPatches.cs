using Game;
using Game.Constants;
using Game.Data;
using Game.Rendering;
using HarmonyLib;
using KL.Grid;
using KL.Utils;
using ModdingOverhauled.Extensions;
using ModdingOverhauled.LayerModule.Definitions;
// ReSharper disable UnusedType.Global
// ReSharper disable InconsistentNaming

namespace ModdingOverhauled.LayerModule.Patches;

public class EntityUtilsPatches {
    [HarmonyPatch(typeof(EntityUtils), nameof(EntityUtils.GetTopmostTile))]
    public static class GetTopMostTilePatches {
        public static bool Prefix(int pos, ref Tile __result) {
            if (!EntityUtils.IsWithinBounds(pos)) return false;
            __result = null;
            var grid = A.S.Map.Grids;
            var clickOrder = LayerDef.LayersBySelectionOrder;
            for (var li = clickOrder.Length - 1; li >= 0; li--) {
                var layerId = clickOrder[li];
                if (layerId < 0) break;
                __result = grid[layerId].Get(pos);
                // DO NOT CHECK FOR CHILDREN HERE!
                if (__result != null && !__result.IsActive) {
                    __result = null;
                }

                if (__result != null) break;
            }

            return false;
        }
    }
    [HarmonyPatch(typeof(EntityUtils), nameof(EntityUtils.CheckIfConstructionIsPossible))]
    public static class CheckIfConstructionIsPossiblePatches {
        [HarmonyPrefix]
        public static bool Prefix(GameState S, Being worker, Tile tile, bool autoAssignFloor, out string fail, ref bool __result) {
            if (tile.Constructable.IsBuiltInSpace) {
                var placement = tile.Constructable != null ? tile.Constructable.Placement : PlacementConstraint.OnFloor;
                if (placement == PlacementConstraint.InSpaceAttached) {
                    if (!EntityUtils.HasAdjacentFloor(tile, tile.PosIdx, true)) {
                        fail = T.AdRejFloorNotConstructed;
                        __result = false;
                        return false;
                    }
                }

                if (placement == PlacementConstraint.InSpaceAttachedChained) {
                    if (!EntityUtils.HasAdjacentFloor(tile, tile.PosIdx, true)
                        && !EntityUtils.HasAdjacentSamePlacementGroup(
                            tile, tile.PosIdx, false)) {
                        fail = T.PlacementTipInspaceattachedchained;
                        __result = false;
                        return false;
                    }
                }

                fail = null;
                __result = true;
                return false;
            }

            if (tile.Transform.IsMulti) {
                var count = tile.Transform.GetSubtilePos(Caches.PosIdx);
                var layerDef = tile.Transform.GetLayerDef();
                var floorMissing = false;
                var floorNotBuilt = false;
                Tile unbuiltFloor = null;
                if (!LayerUtils.HasPlacementRestrictions(layerDef)) {
                    __result = true;
                    fail = null;
                    return false;
                }

                for (var i = 0; i < count; i++) {
                    floorMissing = LayerUtils.GetFloorFor(S, Caches.PosIdx[i], layerDef) == null;
                    unbuiltFloor = LayerUtils.GetBuiltFloorFor(S, Caches.PosIdx[i], layerDef);
                    floorNotBuilt = unbuiltFloor == null;
                    if (floorNotBuilt || floorMissing)
                        break;
                }

                if (floorMissing) {
                    fail = T.AdRejNoFloor;
                    __result = false;
                    if (autoAssignFloor) {
                        EntityUtils.AssignUnbuiltFloorTask(S, unbuiltFloor, worker);
                    }

                    return false;
                }

                if (floorNotBuilt) {
                    __result = false;
                    fail = T.AdRejFloorNotConstructed;
                    return false;
                }
            }
            else {
                var layerDef = tile.Transform.GetLayerDef();
                var unbuiltFloor = LayerUtils.GetBuiltFloorFor(tile.S, tile.PosIdx, layerDef);
                if (unbuiltFloor == null) {
                    var existing = LayerUtils.GetFloorFor(tile.S, tile.PosIdx, layerDef);
                    if (existing == null) {
                        fail = T.AdRejNoFloor;
                        __result = false;
                        return false;
                    }

                    if (autoAssignFloor) {
                        EntityUtils.AssignUnbuiltFloorTask(S, existing, worker);
                    }

                    fail = T.AdRejFloorNotConstructed;
                    __result = false;
                    return false;
                }
            }

            fail = null;
            __result = true;
            return false;
        }
    }
    [HarmonyPatch(typeof(EntityUtils), nameof(EntityUtils.HasAdjacentFloor))]
    public static class HasAdjacentFloorPatches {
        [HarmonyPrefix]
        public static bool Prefix(Tile obj, int objRootPos, bool requireConstructed, ref bool __result) {
            if (obj?.Transform == null) {
                D.Err("Checking has adjacent floor for tile that is null or without transform: {0}", obj);
                __result = false;
                return false;
            }

            if (obj.Transform.IsMulti) {
                if (obj.Transform.IsChild) {
                    D.Err("Reassigning child to parent!");
                    obj = obj.Transform.Parent;
                }
            }

            if (obj.Transform.HasWorkSpot) {
                var workSpot = obj.Transform.RotatedWorkSpot(objRootPos);
                __result = LayerUtils.CanBeBuiltOrPlacedAt(obj.S, workSpot, obj.Transform.LayerId);
                return false;
            }

            D.Ass(objRootPos != Pos.Invalid,
                "Trying to do adjacent floor check for invalid tile pos: {0}", obj);
            Pos.ToXY(objRootPos, out var rX, out var rY);
            for (var x = -1; x <= obj.Transform.RotatedWidth; x++) {
                for (var y = -1; y <= obj.Transform.RotatedHeight; y++) {
                    if (x == -1 || x == obj.Transform.RotatedWidth) {
                        if (y == -1 || y == obj.Transform.RotatedHeight) {
                            continue;
                        }

                        if (LayerUtils.CanBeBuiltOrPlacedAt(obj.S, Pos.FromXY(rX + x, rY + y), obj.Transform.LayerId,
                                out _, out var built)) {
                            if (requireConstructed && built) {
                                __result = true;
                                return false;
                            }
                        }
                    }

                    if (y == -1 || y == obj.Transform.RotatedHeight) {
                        if (x == -1 || x == obj.Transform.RotatedWidth) {
                            continue;
                        }

                        if (LayerUtils.CanBeBuiltOrPlacedAt(obj.S, Pos.FromXY(rX + x, rY + y), obj.Transform.LayerId,
                                out _, out var built)) {
                            if (requireConstructed && built) {
                                __result = true;
                                return false;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}