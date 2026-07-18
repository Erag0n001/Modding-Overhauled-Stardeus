using System.Collections.Generic;
using Game;
using Game.Components;
using Game.Data;
using Game.Systems.Path;
using Game.Systems.UIUX;
using HarmonyLib;
using KL.Grid;
using ModdingOverhauled.LayerModule.Const;
using ModdingOverhauled.LayerModule.Definitions;
using ModdingOverhauled.Logging;
using Unity.Collections;
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local

namespace ModdingOverhauled.LayerModule.Patches;

public static class PathNodePatches {
    [HarmonyPatch(typeof(PathNode), nameof(PathNode.Update))]
    public static class UpdatePatches {
        [HarmonyPrefix]
        public static bool Prefix(ref NativeArray<PathNode> nodes, int posIdx, Dictionary<int, int> tpMap, HashSet<int> workSpots) {
            if (!EntityUtils.IsWithinBounds(posIdx))
                return false;
            var passable = true;
            var anyWalkable = false;
            var totalDifficulty = 0f;
            var totalDeny = NavPermission.None;
            var node = new PathNode();
            node.PosIdx = posIdx;
            node.TeleportsTo = -1;
            var isTeleporter = false;
            var isSolidWall = false;
            var isDoor = false;
            var isTileWithHeight = false;
            foreach (var layerGroup in LayerDef.AffectsPathFindingLayers) {
                // These are already pre-sorted by priority
                foreach (var layer in layerGroup) {
                    var tile = A.S.Grids[layer.LayerId].Get(posIdx);
                    if(tile == null) 
                        continue;
                    var parent = tile.Transform.IsChild ? tile.Transform.Parent : tile;
                    if (!isTileWithHeight) {
                        isTileWithHeight = parent.Graphics.HasHeight
                                           && parent.Passability?.IsUnbuiltOverrideActive != true;
                    }
                    var passability = parent.Passability;
                    GetBasePassability(passability, out var p, out var w, out var d,
                        out var deny);
                    passable &= p;
                    if (layer.PathFindingInfo.LayerTypeH == LayerConstants.FloorHash) {
                        anyWalkable |= w;
                    }

                    totalDifficulty += d;
                    totalDeny |= deny;
                
                    var teleporterComp = tile.GetComponent<TeleporterComp>();
                    if (!isTeleporter && teleporterComp != null) {
                        if (teleporterComp.IsTeleporterActive && tpMap.TryGetValue(posIdx, out var tpTarget)) {
                            isTeleporter = true;
                            node.TeleportsTo = tpTarget;
                        }
                    }
                    
                    if (layer.PathFindingInfo.LayerTypeH == LayerConstants.WallHash) {
                        isDoor = passability?.IsDoor ?? false;
                        isSolidWall = !isDoor && tile.IsConstructed && passability?.IsUnbuiltOverrideActive != true;
                    }
                    break;
                }
            }
            var isWalkable = passable && anyWalkable;
            node.Difficulty = totalDifficulty;
            node.Deny = totalDeny;
            node.Type = PathNodeType.None;
            if (passable) {
                node.Type |= PathNodeType.Passable;
            }

            if (isWalkable) {
                node.Type |= PathNodeType.Walkable;
            }

            if (isDoor) {
                node.Type |= PathNodeType.Door;
            } else if (isSolidWall) {
                node.Type |= PathNodeType.Wall;
            } else if (isTileWithHeight) {
                node.Type |= PathNodeType.TileWithHeight;
            }

            if (isTeleporter) {
                node.Type |= PathNodeType.Teleporter;
            }

            if (workSpots.Contains(posIdx)) {
                node.Type |= PathNodeType.Port;
            }
            
            nodes[posIdx] = node;
            
            return false;
        }

        private static string NodeToString(in PathNode node) {
            return $"diff:{node.Difficulty}|type:{node.Type}|deny:{node.Deny}|tpTo:{node.TeleportsTo}";
        }
        
        private static void GetBasePassability(
            PassabilityComp pass,
            out bool isP,
            out bool isW,
            out float diff,
            out NavPermission deny)
        {
            if (pass == null)
            {
                isP = true;
                isW = false;
                diff = 0.0f;
                deny = NavPermission.None;
            }
            else
            {
                isP = pass.IsPassable;
                isW = pass.IsWalkable;
                diff = pass.Difficulty;
                deny = pass.Deny;
            }
        }
    }
}