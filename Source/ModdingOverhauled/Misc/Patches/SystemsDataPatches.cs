using System.Collections.Generic;
using System.Reflection.Emit;
using Game.Data;
using HarmonyLib;

namespace ModdingOverhauled.Misc.Patches
{
    public static class SystemsDataPatches
    {
        /// <summary>
        /// ModData is by default null. To avoid a null check on every system from every mod, we instead just initialize it empty.
        /// This avoids every system have to explicitly check for null
        /// </summary>
        [HarmonyPatch(typeof(SystemsData), nameof(SystemsData.Serialize))]
        public static class SerializePatch
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
                var helper = AccessTools.Method(typeof(SerializePatch), nameof(Helper));
                for (int i = 0; i < codes.Count; i++)
                {
                    var code = codes[i];
                    if (code.opcode == OpCodes.Ldarg_0)
                    {
                        codes.InsertRange(i, new  CodeInstruction[]
                        {
                            new (OpCodes.Ldloc_0),
                            new (OpCodes.Call, helper)
                        });
                        break;
                    }
                }
                return codes;
            }

            private static void Helper(SystemsData data)
            {
                data.SpecialData.ModData = new Dictionary<string, byte[]>();
            }
        }
    }
}