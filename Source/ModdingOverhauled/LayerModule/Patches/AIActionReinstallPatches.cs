using Game;
using Game.Constants;
using Game.Data;
using Game.Systems.AI;
using Game.Systems.Path;
using HarmonyLib;
using ModdingOverhauled.Extensions;
using ModdingOverhauled.LayerModule.Definitions;
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global

namespace ModdingOverhauled.LayerModule.Patches;

public static class AIActionReinstallPatches {
    [HarmonyPatch(typeof(AIActionReinstall), "CheckReachability")]
    public static class CheckReachabilityPatches {
        [HarmonyPrefix]
        private static bool CheckReachability(AIActionReinstall __instance, GameState S, AIGoal goal, AIAgentComp agent, ref AIActionResult __result) {
            if (__instance.TryGetSourceTarget(goal, out var src, out var target)) {
                const PathReqFlags flags = PathReqFlags.AdjNearby | PathReqFlags.Instant;
                
                if (!agent.Motion.EstimatePathTo(src.Tile, null, flags)) {
                    __result = __instance.FailedNavToTarget(agent, src.Tile.PosIdx);
                    return false;
                }
                
                var targetReachable = agent.Motion.EstimatePathTo(target.Tile, null, flags);
                if (!targetReachable) {
                    if (!S.Sys.Areas.IsSameAreaIndoors(
                            src.Tile.PosIdx, target.Tile.PosIdx)) {
                        agent.Motion.StoreNavFailureAt(target.Tile.PosIdx);
                        __result = __instance.FailedNavToTarget(agent, target.Tile.PosIdx);
                        return false;
                    }
                }

                if (!src.Tile.Constructable.IsBuiltInSpace) {
                    var cnt = target.Tile.Transform.GetSubtilePos(Caches.IntArray128);
                    var layerDef = src.Tile.Transform.GetLayerDef();
                    var floorMissing = false;
                    var floorNotBuilt = false;
                    if (!LayerUtils.HasPlacementRestrictions(layerDef)) {
                        __instance.SetPhase(agent, AIActionReinstall.Phase.ReachSource);
                        __result = AIActionResult.Create(AIActionState.InProgress);
                        return false;
                    }

                    for (var i = 0; i < cnt; i++) {
                        floorMissing = LayerUtils.GetFloorFor(S, Caches.IntArray128[i], layerDef) == null;
                        floorNotBuilt = LayerUtils.GetBuiltFloorFor(S, Caches.IntArray128[i], layerDef) == null;
                        if (floorMissing || floorNotBuilt)
                            break;
                    }


                    if (floorMissing) {
                        AIGoal.Cancel(goal, G.Ticks, T.AdRejNoFloor, removeImmediately: false);
                        __result = AIActionResult.Create(AIActionState.Failed, T.AdRejNoFloor);
                        return false;
                    }

                    if (floorNotBuilt) {
                        __result = AIActionResult.Create(AIActionState.Failed, T.AdRejFloorNotConstructed);
                        return false;
                    }
                }

                __instance.SetPhase(agent, AIActionReinstall.Phase.ReachSource);
                __result = AIActionResult.Create(AIActionState.InProgress);
                return false;
            }

            __result = AIActionResult.Create(AIActionState.Failed, T.AdRejTargetUnavailable);
            return false;
        }
    }
}