using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Game.Components;
using HarmonyLib;
using ModdingOverhauled.LayerModule.Definitions;

namespace ModdingOverhauled.LayerModule.Patches;

public static class ConstructableCompPatches {
    [HarmonyPatch(typeof(ConstructableComp), nameof(ConstructableComp.FinishConstruction))]
    public static class FinishConstructionPatches {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator ilGenerator) {
            var codes = new List<CodeInstruction>(instructions);
            var wasPatched = false;
            var i = 0;
            var toLookFor = AccessTools.PropertyGetter(typeof(BaseComponent<ConstructableComp>), nameof(BaseComponent<>.LayerId));
            var helper = AccessTools.Method(typeof(FinishConstructionPatches), nameof(Helper));
            for (; i < codes.Count; i++) {
                var c = codes[i];
                if (c.opcode != OpCodes.Call) 
                    continue;
                if (c.operand is not MethodInfo method || method != toLookFor) 
                    continue;
                var wallLayerCode = codes[i + 1];
                if (wallLayerCode.opcode != OpCodes.Ldc_I4_1) 
                    continue;
                
                var label = (Label)codes[i + 2].operand;
                // We remove the last 3 calls, leaving the layer id on the stack
                codes.RemoveRange(i + 1, 2);
                codes.InsertRange(i + 1, new CodeInstruction[] {
                    new(OpCodes.Call, helper),
                    new(OpCodes.Brfalse, label),
                });
                wasPatched = true;
                break;
            }

            if (!wasPatched) {
                throw new Exception($"Failed to patch {typeof(FinishConstructionPatches)}");
            }
            return codes;
        }

        private static bool Helper(int layerId) {
            return LayerDef.AllByLayerId[layerId].IsConnected;
        }
    }
}