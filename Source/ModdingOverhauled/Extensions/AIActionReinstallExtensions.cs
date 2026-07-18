using System;
using Game.Data.Space;
using Game.Systems.AI;
using HarmonyLib;
using ModdingOverhauled.Utils;

namespace ModdingOverhauled.Extensions;

public static class AIActionReinstallExtensions {
    private static readonly Func<AIActionReinstall, AIAgentComp, bool, AIActionResult> FailedToNaveToTargetDelegate = 
    (Func<AIActionReinstall, AIAgentComp, bool, AIActionResult>) 
    ReflectionUtilities.CreateMethodCall(AccessTools.Method(typeof(AIActionReinstall), "FailedNavToTarget"));
    private static readonly Func<AIActionReinstall, AIAgentComp, int, bool, AIActionResult> FailedToNaveToTargetDelegate2 = 
        (Func<AIActionReinstall, AIAgentComp, int, bool, AIActionResult>) 
        ReflectionUtilities.CreateMethodCall(AccessTools.Method(typeof(AIActionReinstall), "FailedNavToTarget"));

    public static AIActionResult FailedNavToTarget(this AIActionReinstall action, AIAgentComp comp, int posIdx,
        bool errorOnInvalidPos = true) {
        return FailedToNaveToTargetDelegate2(action, comp, posIdx, errorOnInvalidPos);
    }
}