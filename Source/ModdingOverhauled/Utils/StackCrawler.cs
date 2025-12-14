using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ModdingOverhauled.Extensions;
using ModdingOverhauled.Logging;

namespace Testing;

/// <summary>
/// Harmony transpiler utility, capable of doing safe operations onto a method's body without deep IL knowledge.
/// It can also be used to test the output IL with a more detailed breakdown of why it broke.
/// </summary>
public partial class StackCrawler
{
    public readonly MethodInfo CurrentMethod;

    public readonly List<Type> CurrentParameters;

    public readonly IList<LocalVariableInfo> Locals;

    public readonly ILGenerator Il;

    public readonly Dictionary<Label, int> LabelToIndex = new Dictionary<Label, int>();
    
    public List<CodeInstruction> All;

    public List<InstructionGroup> Groups = new List<InstructionGroup>();
    
    public StackCrawler(IEnumerable<CodeInstruction> instructions, MethodInfo baseMethod, ILGenerator gen)
    {
        All = instructions.ToList();
        List<Type> parameters = new List<Type>();
        if(!baseMethod.IsStatic)
            parameters.Add(baseMethod.DeclaringType!);
        foreach (var parameter in baseMethod.GetParameters())
        {
            if (parameter.ParameterType.IsByRef)
                parameters.Add(parameter.ParameterType.GetElementType()!);
            else
                parameters.Add(parameter.ParameterType);
        }
        Il = gen;
        CurrentMethod = baseMethod;
        CurrentParameters = parameters;
        var methodBody = baseMethod.GetMethodBody();
        Locals = methodBody.LocalVariables;
        RecomputeGroups();
    }

    internal static readonly OpCode[] CallOpCodes =
    [
        OpCodes.Newobj,
        OpCodes.Call,
        OpCodes.Callvirt,
        OpCodes.Calli
    ];
    
    /// <summary>
    /// 2 Pop, 1 push
    /// </summary>
    internal static readonly OpCode[] MathOpCodes =
    [
        OpCodes.Div,
        OpCodes.Div_Un,
        OpCodes.Mul,
        OpCodes.Mul_Ovf,
        OpCodes.Mul_Ovf_Un,
        OpCodes.Rem,
        OpCodes.Rem_Un,
        OpCodes.Add,
        OpCodes.Add_Ovf,
        OpCodes.Add_Ovf_Un
    ];

    internal static readonly OpCode[] UnConditionalBrOpCodes =
    [
        OpCodes.Br,
        OpCodes.Br_S,
        OpCodes.Brfalse,
        OpCodes.Brfalse_S,
        OpCodes.Brtrue,
        OpCodes.Brtrue_S,
        OpCodes.Leave,
        OpCodes.Leave_S
    ];
    
    // pop 2, push 0, transfer control
    internal static readonly OpCode[] ConditionalBrOpCodes =
    {
        OpCodes.Beq, OpCodes.Beq_S,
        OpCodes.Bne_Un, OpCodes.Bne_Un_S,
        OpCodes.Bge, OpCodes.Bge_S,
        OpCodes.Bge_Un, OpCodes.Bge_Un_S,
        OpCodes.Bgt, OpCodes.Bgt_S,
        OpCodes.Bgt_Un, OpCodes.Bgt_Un_S,
        OpCodes.Ble, OpCodes.Ble_S,
        OpCodes.Ble_Un, OpCodes.Ble_Un_S,
        OpCodes.Blt, OpCodes.Blt_S,
        OpCodes.Blt_Un, OpCodes.Blt_Un_S
    };
    
    internal static readonly OpCode[] LoadFieldOpCodes =
    [
        OpCodes.Ldsfld,
        OpCodes.Ldsflda,
        OpCodes.Ldfld,
        OpCodes.Ldflda,
        OpCodes.Ldftn
    ];
    
    internal static readonly OpCode[] GetLocalOpCodes =
    [
        OpCodes.Ldloc,
        OpCodes.Ldloc_0,
        OpCodes.Ldloc_1,
        OpCodes.Ldloc_2,
        OpCodes.Ldloc_3,
        OpCodes.Ldloc_S,
    ];
    
    internal static readonly OpCode[] SetLocalOpCodes =
    [
        OpCodes.Stloc,
        OpCodes.Stloc_0,
        OpCodes.Stloc_1,
        OpCodes.Stloc_2,
        OpCodes.Stloc_3,
        OpCodes.Stloc_S,
    ];
    
    internal static readonly OpCode[] SetFieldOpCodes =
    [
        OpCodes.Stsfld,
        OpCodes.Stfld
    ];

    internal static readonly OpCode[] GetArgumentOpCodes =
    [
        OpCodes.Ldarg,
        OpCodes.Ldarg_0,
        OpCodes.Ldarg_1,
        OpCodes.Ldarg_2,
        OpCodes.Ldarg_3,
        OpCodes.Ldarg_S,
        OpCodes.Ldarga,
        OpCodes.Ldarga_S,
    ];

    internal static readonly Dictionary<OpCode, Type> ConsOpCodesToType = new()
    {
        { OpCodes.Ldc_I4_M1, typeof(int) },
        { OpCodes.Ldc_I4_0, typeof(int) },
        { OpCodes.Ldc_I4_1, typeof(int) },
        { OpCodes.Ldc_I4_2, typeof(int) },
        { OpCodes.Ldc_I4_3, typeof(int) },
        { OpCodes.Ldc_I4_4, typeof(int) },
        { OpCodes.Ldc_I4_5, typeof(int) },
        { OpCodes.Ldc_I4_6, typeof(int) },
        { OpCodes.Ldc_I4_7, typeof(int) },
        { OpCodes.Ldc_I4_8, typeof(int) },

        { OpCodes.Ldc_I4_S, typeof(int) },
        { OpCodes.Ldc_I4, typeof(int) },
        { OpCodes.Ldc_I8, typeof(long) },
        
        { OpCodes.Ldc_R4, typeof(float) },
        { OpCodes.Ldc_R8, typeof(double) },
        
        { OpCodes.Ldstr, typeof(string) },
        
        { OpCodes.Ldnull, typeof(object) }
    };
    
    /// <summary>
    /// Recomputes the groups. Should be done after each change
    /// </summary>
    public void RecomputeGroups()
    {
        Groups.Clear();
        for (int x = 0; x < All.Count; x++)
        {
            var labels = All[x].labels;
            if (labels != null && labels.Count > 0)
            {
                foreach(var label in labels)
                {
                    LabelToIndex.TryAdd(label, x);
                }
            }
        }
        for (int i = 0; i < All.Count;)
        {
            var currentGroup = new InstructionGroup
            {
                StartingIndex = i
            };

            int stack = 0;

            do
            {
                HandleInstruction(All[i], ref stack, currentGroup);
                i++;
            } while (stack > 0 && i < All.Count);

            Groups.Add(currentGroup);
        }
    }

    /// <summary>
    /// Removes a method
    /// </summary>
    /// <param name="method">Method to remove</param>
    /// <param name="amountToSkip">Amount of calls to the method to skip</param>
    /// <param name="shouldBreak">Should the program stop after finding the first instance</param>
    public void RemoveCallToMethod(MethodBase method, int amountToSkip = 0, bool shouldBreak = false)
    {
        var skipped = 0;
        foreach (var group in Groups.ToArray())
        {
            if (group.MethodsCalled.Contains(method))
            {
                if (skipped < amountToSkip)
                {
                    skipped++;
                    continue;
                }
                All.RemoveRange(group.StartingIndex, group.Instructions.Count);
                Groups.Remove(group);
                if (shouldBreak)
                    break;
            }
        }
        RecomputeGroups();
    }

    /// <summary>
    /// Replaces a constant with another
    /// </summary>
    /// <param name="oldValue">Value to look for</param>
    /// <param name="newValue">Value to insert</param>
    /// <param name="amountToSkip">Amount of calls to the method to skip</param>
    /// <param name="shouldBreak">Should the program stop after finding the first instance</param>
    public void ReplaceConstant<T>(T oldValue, T newValue, int amountToSkip = 0, bool shouldBreak = false)
    {
        var skipped = 0;
        for (int i = 0; i < All.Count; i++)
        {
            var c = All[i];
            if (c.operand != null && c.operand.Equals(oldValue))
            {
                if (skipped < amountToSkip)
                {
                    skipped++;
                    continue;
                }
                c.operand = newValue;
                if (shouldBreak)
                    break;
            }
        }
    }

    /// <summary>
    /// Replaces a constant int32 with another
    /// </summary>
    /// <param name="oldValue">Value to look for</param>
    /// <param name="newValue">Value to insert</param>
    /// <param name="amountToSkip">Amount of calls to the method to skip</param>
    /// <param name="shouldBreak">Should the program stop after finding the first instance</param>
    public void ReplaceConstant(int oldValue, int newValue, int amountToSkip = 0, bool shouldBreak = false)
    {
        var skipped = 0;
        for (int i = 0; i < All.Count; i++)
        {
            var c = All[i];
            if (TryConvertOperandToInt(c, out var result) && result == oldValue)
            {
                if (skipped < amountToSkip)
                {
                    skipped++;
                    continue;
                }
                if (newValue >= -1 && newValue <= 8)
                    c.opcode = newValue switch
                    {
                        -1 => OpCodes.Ldc_I4_M1,
                        0 => OpCodes.Ldc_I4_0,
                        1 => OpCodes.Ldc_I4_1,
                        2 => OpCodes.Ldc_I4_2,
                        3 => OpCodes.Ldc_I4_3,
                        4 => OpCodes.Ldc_I4_4,
                        5 => OpCodes.Ldc_I4_5,
                        6 => OpCodes.Ldc_I4_6,
                        7 => OpCodes.Ldc_I4_7,
                        8 => OpCodes.Ldc_I4_8,
                        _ => throw new ArgumentOutOfRangeException(nameof(newValue), newValue, null)
                    };
                else if (newValue >= sbyte.MinValue && newValue <= sbyte.MaxValue)
                {
                    c.opcode = OpCodes.Ldc_I4_S;
                    c.operand = newValue;
                }
                else
                {
                    c.opcode = OpCodes.Ldc_I4;
                    c.operand = newValue;
                }

                if (shouldBreak)
                    break;
            }
        }
    }


    /// <summary>
    /// Replaces a target method to your method. The method's arguments and return type should match for this
    /// </summary>
    /// <param name="toFind"> Method to replace</param>
    /// <param name="replaceWith">Method to inject</param>
    /// <param name="amountToSkip">Amount of calls to the method to skip</param>
    /// <param name="shouldBreak">Should the program stop after finding the first instance</param>
    /// <exception cref="Exception">Methods do not match</exception>
    public void ReplaceCallToMethod(MethodInfo toFind, MethodInfo replaceWith, int amountToSkip = 0, bool shouldBreak = false)
    {
        ReplaceCallToMethod(toFind, replaceWith, [],  amountToSkip, shouldBreak);
    }

    /// <summary>
    /// Replaces a target method to your method. The method's arguments and return type should match for this
    /// </summary>
    /// <param name="toFind"> Method to replace</param>
    /// <param name="replaceWith">Method to inject</param>
    /// <param name="getArg0">Series of instructions on how to get arg0 of the new method, or "this"</param>
    /// <param name="amountToSkip">Amount of calls to the method to skip</param>
    /// <param name="shouldBreak">Should the program stop after finding the first instance</param>
    /// <exception cref="Exception">Methods do not match</exception>
    public void ReplaceCallToMethod(MethodInfo toFind, MethodInfo replaceWith, CodeInstruction[] getArg0, int amountToSkip = 0, bool shouldBreak = false)
    {
        var originalArgs = toFind.GetParameters().ToList();
        var replacementArgsWithDefault = replaceWith.GetParameters().Where(x => x.HasDefaultValue).Reverse().ToList();
        var replacementArgsWithoutDefault =  replaceWith.GetParameters().Where(x => !x.HasDefaultValue).ToList();
        if (originalArgs.Count != replacementArgsWithoutDefault.Count)
            throw new Exception($"Method {replaceWith.Name} does not match the arguments of {toFind.Name}");
        
        for (int i = 0; i < originalArgs.Count; i++)
        {
            if (originalArgs[i].ParameterType != replacementArgsWithoutDefault[i].ParameterType)
                throw new Exception($"Argument {i} type mismatch: {originalArgs[i].ParameterType} vs {replacementArgsWithoutDefault[i].ParameterType}");
        }
        
        if (toFind.ReturnType != replaceWith.ReturnType)
        {
            throw new Exception($"Method {toFind.Name} does not match the return type of {replaceWith.Name}");
        }

        var skipped = 0;
        
        for (var index = 0; index < All.Count; index++)
        {
            var c = All[index];
            if (c.operand is MethodInfo method && method == toFind)
            {
                if (skipped < amountToSkip)
                {
                    skipped++;
                    continue;
                }

                c.operand = replaceWith;
                var group = GetGroupAt(index);
                int x = group.StartingIndex;
                if (!method.IsStatic)
                {
                    int stack = 0;
                    while (x < All.Count && stack < 1)
                    {
                        ComputeStackChange(All[x], ref stack);
                        x++;
                    }
                    All.RemoveRange(group.StartingIndex, x - group.StartingIndex);
                }
                if (x <= All.Count)
                {
                    All.InsertRange(group.StartingIndex, getArg0);
                }
                
                if (replacementArgsWithDefault.Count > 0)
                {
                    index += getArg0.Length;
                    foreach (var arg in replacementArgsWithDefault)
                    {
                        var defaultValue = arg.DefaultValue;
                        if (defaultValue == null) All.Insert(index, new CodeInstruction(OpCodes.Ldnull));
                        else if (defaultValue is int intVal) All.Insert(index, new(OpCodes.Ldc_I4, intVal));
                        else if (defaultValue is long longVal) All.Insert(index, new(OpCodes.Ldc_I8, longVal));
                        else if (defaultValue is float floatVal) All.Insert(index, new(OpCodes.Ldc_R4, floatVal));
                        else if (defaultValue is double doubleVal) All.Insert(index, new(OpCodes.Ldc_R8, doubleVal));
                        else if (defaultValue is string str) All.Insert(index, new(OpCodes.Ldstr, str));
                        else throw new NotImplementedException($"Default value type {arg.ParameterType} not handled");
                        index++;
                    }
                }

                if (shouldBreak)
                    break;
            }
        }
        RecomputeGroups();
    }

    /// <summary>
    /// Prints the entire method's body
    /// </summary>
    public void PrintCode()
    {
        Console.WriteLine(string.Join("\n", All));
    }

    internal InstructionGroup GetGroupAt(int index)
    {
        return Groups.FirstOrDefault(x => x.StartingIndex <= index && x.Instructions.Count + x.StartingIndex >= index)!;
    }

    private int ComputeStackAt(int index)
    {
        int stack = 0;
        for (int i = 0; i < index; i++)
        {
            var c = All[i];
            ComputeStackChange(c, ref stack);
        }
        return stack;
    }
    
    private void HandleInstruction(CodeInstruction c, ref int stack, InstructionGroup currentGroup)
    {
        currentGroup.Instructions.Add(c);
        ComputeStackChange(c, ref stack);
        
        if (CallOpCodes.Contains(c.opcode))
        {
            HandleMethodCall(c, currentGroup);
            return;
        }

        if (LoadFieldOpCodes.Contains(c.opcode))
        {
            currentGroup.FieldsGet.Add((FieldInfo)c.operand);
            return;
        }

        if (SetFieldOpCodes.Contains(c.opcode))
        {
            currentGroup.FieldsSet.Add((FieldInfo)c.operand);
        }
 
    }
    
    private void HandleMethodCall(CodeInstruction code, InstructionGroup currentGroup)
    {
        var method = code.operand as MethodBase;
        if (method == null)
        {
            throw new Exception($"Wtf");
        }
        currentGroup.MethodsCalled.Add(method);
    }
    
    private void ComputeStackChange(CodeInstruction c, ref int stack)
    {
        if (CallOpCodes.Contains(c.opcode))
        {
            var method = (MethodBase)c.operand;
            if (method is MethodInfo mi && mi.ReturnType != typeof(void))
                stack++;
        
            if (!method.IsStatic && !method.IsConstructor)
                stack--;
        
            var args = method.GetParameters();
            stack -= args.Length;
        }
        else if (c.opcode == OpCodes.Ret)
        {
            stack = 0;
        }
        else
        {
            stack += c.opcode.GetStackDelta();
        }
    }

    private bool TryConvertOperandToInt(CodeInstruction c, out int value)
    {
        if (c.opcode == OpCodes.Ldc_I4_0)
        {
            value = 0;
            return true;
        }

        if (c.opcode == OpCodes.Ldc_I4_1)
        {
            value = 1;
            return true;
        }

        if (c.opcode == OpCodes.Ldc_I4_2)
        {
            value = 2;
            return true;
        }
        
        if (c.opcode == OpCodes.Ldc_I4_3)
        {
            value = 3;
            return true;
        }
        
        if (c.opcode == OpCodes.Ldc_I4_4)
        {
            value = 4;
            return true;
        }
        
        if (c.opcode == OpCodes.Ldc_I4_5)
        {
            value = 5;
            return true;
        }
        
        if (c.opcode == OpCodes.Ldc_I4_6)
        {
            value = 6;
            return true;
        }
        
        if (c.opcode == OpCodes.Ldc_I4_7)
        {
            value = 7;
            return true;
        }
        
        if (c.opcode == OpCodes.Ldc_I4_8)
        {
            value = 8;
            return true;
        }
        
        if (c.opcode == OpCodes.Ldc_I4_M1)
        {
            value = -1;
            return true;
        }

        if (c.operand is sbyte b)
        {
            value = b;
            return true;
        }

        if (c.operand is int i)
        {
            value = i;
        }
        
        value = int.MinValue;
        return false;
    }
    
    private class StackCounter(StackCrawler parent)
    {
        private StackCrawler Parent = parent;
        private int StartingIndex;
        private int StartingStack;

        public void Count()
        {
            for (int i = StartingIndex; i < Parent.All.Count;)
            {
                var currentGroup = new InstructionGroup
                {
                    StartingIndex = i
                };

                int stack = 0;

                do
                {
                    var c = Parent.All[i];
                    parent.HandleInstruction(c, ref stack, currentGroup);
                    i++;
                    if (c.opcode == OpCodes.Br || c.opcode == OpCodes.Br_S)
                    {
                        Make(Parent.LabelToIndex[(Label)c.operand], stack).Count();
                    }
                } while (stack > 0 && i < Parent.All.Count);

                parent.Groups.Add(currentGroup);
            }
        }

        public StackCounter Make(int startingIndex, int startingStack)
        {
            StackCounter copy = new StackCounter(Parent);
            copy.StartingIndex = startingIndex;
            copy.StartingStack = startingStack;
            return copy;
        }
    }
}

public class InstructionGroup
{
    public int StartingIndex;
    public List<CodeInstruction> Instructions = new List<CodeInstruction>();
    public List<MethodBase> MethodsCalled = new List<MethodBase>();
    public List<FieldInfo> FieldsGet = new List<FieldInfo>();
    public List<FieldInfo> FieldsSet = new List<FieldInfo>();

    public override string ToString()
    {
        string result = StartingIndex.ToString();
        if (MethodsCalled.Count > 0)
            result += $"|{MethodsCalled.Last().Name}";
        else
            result += $"|{Instructions.Last()}";
        return result;
    }
}