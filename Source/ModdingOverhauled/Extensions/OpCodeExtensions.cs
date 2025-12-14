using System.Reflection.Emit;
using ModdingOverhauled.Logging;

namespace ModdingOverhauled.Extensions;

public static class OpCodeExtensions
{
    public static int GetStackDelta(this OpCode opcode)
    {
        int pop = GetStackBehaviourCount(opcode.StackBehaviourPop);
        int push = GetStackBehaviourCountPush(opcode.StackBehaviourPush);
        return push - pop;
    }

    private static int GetStackBehaviourCount(StackBehaviour behaviour)
    {
        switch (behaviour)
        {
            case StackBehaviour.Pop0: return 0;
            case StackBehaviour.Pop1:
            case StackBehaviour.Popi:
            case StackBehaviour.Popref: return 1;
            case StackBehaviour.Pop1_pop1:
            case StackBehaviour.Popi_pop1:
            case StackBehaviour.Popi_popi:
            case StackBehaviour.Popi_popi8:
            case StackBehaviour.Popi_popr4:
            case StackBehaviour.Popi_popr8:
            case StackBehaviour.Popref_pop1:
            case StackBehaviour.Popref_popi: return 2;
            case StackBehaviour.Popi_popi_popi:
            case StackBehaviour.Popref_popi_popi:
            case StackBehaviour.Popref_popi_popi8:
            case StackBehaviour.Popref_popi_popr4:
            case StackBehaviour.Popref_popi_popr8:
            case StackBehaviour.Popref_popi_popref: return 3;
            case StackBehaviour.Varpop: return -1; // special handling (call, calli, etc.)
            default: return 0;
        }
    }

    private static int GetStackBehaviourCountPush(StackBehaviour behaviour)
    {
        switch (behaviour)
        {
            case StackBehaviour.Push0: return 0;
            case StackBehaviour.Push1:
            case StackBehaviour.Pushi:
            case StackBehaviour.Pushi8:
            case StackBehaviour.Pushr4:
            case StackBehaviour.Pushr8:
            case StackBehaviour.Pushref: return 1;
            case StackBehaviour.Push1_push1: return 2;
            case StackBehaviour.Varpush: return -1; // special handling
            default: return 0;
        }
    }

}