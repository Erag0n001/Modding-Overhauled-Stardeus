using System.Reflection.Emit;

namespace ModdingOverhauled.Extensions;

public static class OpcodeExtensions {
    public static int GetStackDelta(this OpCode opcode) {
        var delta = 0;
        if (opcode.StackBehaviourPop != StackBehaviour.Pop0) {
            var popName = opcode.StackBehaviourPop.ToString().Split('_');
            delta -= popName.Length;
        }

        if (opcode.StackBehaviourPush != StackBehaviour.Push0) {
            var pushName = opcode.StackBehaviourPush.ToString().Split('_');
            delta += pushName.Length;
        }

        return delta;
    }
}