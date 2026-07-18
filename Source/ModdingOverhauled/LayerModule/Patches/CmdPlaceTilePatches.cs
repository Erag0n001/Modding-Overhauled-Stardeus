using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Game;
using Game.Commands;
using Game.Constants;
using Game.Data;
using Game.Utils;
using HarmonyLib;
using ModdingOverhauled.LayerModule.Const;
using ModdingOverhauled.LayerModule.Definitions;
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
// ReSharper disable InconsistentNaming

namespace ModdingOverhauled.LayerModule.Patches;

public static class CmdPlaceTilePatches {
    [HarmonyPatch(typeof(CmdPlaceTile), "CanPlacePartial")]
    public static class CanPlacePartialPatches {
        [HarmonyTranspiler]
        [HarmonyDebug]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator ilGenerator) {
            var codes = new List<CodeInstruction>(instructions);
            var i = 0;
            PatchPlacementRestrictions(ref i, codes);
            PatchHasAdjacentFloor(ref i, codes);
            PatchHardcodedLayerCheckForBlocking(ref i, codes);
            return codes;
        }

        private static void PatchPlacementRestrictions(ref int i, List<CodeInstruction> codes) {
            var wasPatched = false;
            var toCall = AccessTools.Method(typeof(CanPlacePartialPatches), nameof(CheckIfLayerCanBePlacedOn));
            
            for (; i < codes.Count; i++) {
                var code = codes[i];
                if (code.opcode == OpCodes.Stloc_1) {
                    codes.RemoveRange(i - 10, 11);
                    codes.InsertRange(i - 10, [
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldarg_2),
                        new CodeInstruction(OpCodes.Ldarg_3),
                        new CodeInstruction(OpCodes.Ldarg_S, 6),
                        new CodeInstruction(OpCodes.Ldarg_S, 7),
                        new CodeInstruction(OpCodes.Call, toCall),
                        new CodeInstruction(OpCodes.Stloc_1)
                    ]);
                    wasPatched = true;
                    break;
                }
            }

            if (!wasPatched) {
                throw new Exception($"Failed to patch {nameof(CanPlacePartialPatches)}");
            }
        }

        private static void PatchHasAdjacentFloor(ref int i, List<CodeInstruction> codes) {
            var wasPatched = false;
            var methodToLookFor = AccessTools.Method(typeof(EntityUtils), nameof(EntityUtils.HasAdjacentFloor));
            var methodToCall = AccessTools.Method(typeof(LayerUtils), nameof(LayerUtils.HasAdjacentSupport));
            for (; i < codes.Count; i++) {
                var code = codes[i];
                if (code.operand is MethodInfo method && method == methodToLookFor) {
                    if (codes[i - 1].opcode == OpCodes.Ldc_I4_0) {
                        codes.RemoveRange(i - 1, 2);
                        codes.Insert(i - 1, new CodeInstruction(OpCodes.Call, methodToCall));
                        wasPatched = true;
                        break;
                    }
                }
            }

            if (!wasPatched) {
                throw new Exception($"Failed to patch {nameof(PatchPlacementRestrictions)}");
            }
        }
        
        private static void PatchHardcodedLayerCheckForBlocking(ref int i, List<CodeInstruction> codes) {
            var wasPatched = false;
            var toCall = AccessTools.Method(typeof(CanPlacePartialPatches), nameof(CheckIfBlocked));
            Label toJumpIfNotBlocked = default;
            var startIndex = 0;
            List<Label> startLabel = null;
            for (; i < codes.Count; i++) {
                var code = codes[i];
                if (code.opcode == OpCodes.Blt || code.opcode == OpCodes.Blt_S) {
                    if (codes[i - 1].opcode == OpCodes.Ldc_I4_1) {
                        if (codes[i - 2].opcode == OpCodes.Ldarg_3) {
                            startIndex = i - 2;
                            startLabel = codes[i - 2].labels;
                            toJumpIfNotBlocked = (Label)code.operand;
                            break;
                        }
                    }
                }
            }

            for (; i < codes.Count; i++) {
                var code = codes[i];
                if (code.labels.Contains(toJumpIfNotBlocked)) {
                    codes.RemoveRange(startIndex, i - startIndex);
                    const int canReplaceArg = 4;
                    codes.InsertRange(startIndex , [
                        new CodeInstruction(OpCodes.Ldarg_0).WithLabels(startLabel),
                        new CodeInstruction(OpCodes.Ldarg_2),
                        new CodeInstruction(OpCodes.Ldarg_3),
                        new CodeInstruction(OpCodes.Ldarg_S, canReplaceArg),
                        new CodeInstruction(OpCodes.Call, toCall),
                        new CodeInstruction(OpCodes.Brtrue, toJumpIfNotBlocked),
                        new CodeInstruction(OpCodes.Ldc_I4_0),
                        new CodeInstruction(OpCodes.Ret)
                    ]);
                    wasPatched = true;
                    break;
                }
            }
            
            if (!wasPatched) {
                throw new Exception($"Failed to patch first step of {typeof(CanPlacePartialPatches)}");
            }

        }

        public static Tile CheckIfLayerCanBePlacedOn(Tile tile, int posIdx, int layer, PlacementConstraint placement, bool warn) {
            var def = LayerDef.AllByLayerId[layer];
            foreach (var canBePlacedOn in def.CanBePlacedOn) {
                var existing = tile.S.Grids[canBePlacedOn.LayerId].Get(posIdx);
                if (existing != null) {
                    return existing;
                }
            }
            return null;
        }
        
        public static bool CheckIfBlocked(Tile tile, int posIdx, int layer, bool canReplace) {
            var layerDef = LayerDef.AllByLayerId[layer];
            var existing = tile.S.Grids[layer].Get(posIdx);
            if (existing != null && !canReplace) {
                return false;
            }
            foreach (var blockerDef in layerDef.BlockedBy) {
                existing = tile.S.Grids[blockerDef.LayerId].Get(posIdx);
                if (existing != null) {
                    A.S.Sig.ShowErrorMessage.Send($"{T.OperationForbidden}: {T.WarningBlockedBy_V1.F(existing.NameLink)}");
                    return false;
                }
            }
            
            return true;
        }
    }

    [HarmonyPatch(typeof(CmdPlaceTile), "CanPlacePort")]
    public static class CanPlacePortPatches {
        [HarmonyPrefix]
        public static bool Prefix(int portPos, Tile reinstalling, bool allowInWorkSpotCompatible, ref bool __result) {
            if (EntityUtils.IsWallAt(portPos)) { return false; }
            foreach (var layer in LayerDef.AffectsPathFindingLayers[LayerConstants.FloorPathfindingIndex]) {
                var tileAt = A.S.Grids[layer.LayerId].Get(portPos);

                if (tileAt?.Passability?.IsWalkableDefault != true) {
                    continue;
                }
            }
            var obj = EntityUtils.ObjAt(portPos);
            if (obj == null) {
                __result = true;
                return false;
            }
            if (allowInWorkSpotCompatible && obj.Transform.IsWorkSpotCompatible) {
                __result = true;
                return false;
            }
            if (reinstalling != null) {
                if (EntityUtils.ParentObj(obj) == reinstalling) {
                    __result = true;
                    return false;
                }
            }
            return false;
        }
    }
}