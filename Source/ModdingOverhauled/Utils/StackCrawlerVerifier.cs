using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ModdingOverhauled.Logging;

namespace Testing;

public partial class StackCrawler
{
    /// <summary>
    /// Verifies the stack across the entire method.
    /// </summary>
    /// <exception cref="Exception">Stack was unbalanced</exception>
    public void VerifyCode()
    {
        Exception ex = null;
        List<Type> stackTypes = new List<Type>();
        Dictionary<Label, LabelStatus> Labels = new Dictionary<Label, LabelStatus>();
        for (int i = 0; i < All.Count; i++)
        {
            var c = All[i];

            // Needs to be at the top
            if (HandleLabel(c, out ex))
            {
                if (ex != null)
                {
                    throw new Exception($"Group {GetGroupAt(i)} at code {All[i]} ({i}) returned {ex}");
                }
            }
            if (HandleLocals(c, out ex))
            {
                if (ex != null)
                {
                    throw new Exception($"Group {GetGroupAt(i)} at code {All[i]} ({i}) returned {ex}");
                }
                continue;
            }
            if (HandleArg(c, out ex))
            {
                if (ex != null)
                {
                    throw new Exception($"Group {GetGroupAt(i)} at code {All[i]} ({i}) returned {ex}");
                }
                continue;
            }
            if(HandleField(c))
                continue;

            if (HandleCall(c, out ex))
            {
                if (ex != null)
                {
                    throw new Exception($"Group {GetGroupAt(i)} at code {All[i]} ({i}) returned {ex}");
                }
                continue;
            }
            
            if(ConsOpCodesToType.TryGetValue(c.opcode, out var t))
            {
                stackTypes.Add(t);
                continue;
            }

            if (c.opcode == OpCodes.Dup)
            {
                stackTypes.Add(stackTypes.Last());
                continue;
            }

            if (c.opcode == OpCodes.Pop)
            {
                stackTypes.Remove(stackTypes.Last());
                continue;
            }

            if (c.opcode == OpCodes.Ldnull)
            {
                stackTypes.Add(typeof(Null));
                continue;
            }

            if (c.opcode == OpCodes.Ret)
            {
                var returnType = CurrentMethod.ReturnType;
                if (returnType == typeof(void))
                {
                    if (stackTypes.Count > 0)
                    {
                        throw new Exception($"Group {GetGroupAt(i)} at code {All[i]} ({i}) tried to return {returnType} on a void method");
                    }
                }
                else
                {
                    if (stackTypes.Count > 1)
                    {
                        throw new Exception($"Group {GetGroupAt(i)} at code {All[i]} ({i}) tried to return {string.Join(",", stackTypes)} types");
                    }

                    if (stackTypes.Last() != returnType)
                    {
                        throw new Exception($"Group {GetGroupAt(i)} at code {All[i]} ({i}) tried to return {stackTypes.Last()} on a {returnType} method");
                    }
                }

                stackTypes.Remove(stackTypes.Last());
                continue;
            }
            
            if (HandleBox(c, out ex))
            {
                if (ex != null)
                {
                    throw new Exception($"Group {GetGroupAt(i)} at code {All[i]} ({i}) returned {ex}");
                }
                continue;
            }

            if (HandleUnbox(c, out ex))
            {
                if (ex != null)
                {
                    throw new Exception($"Group {GetGroupAt(i)} at code {All[i]} ({i}) returned {ex}");
                }
            }
            
            if (HandleUnboxAny(c, out ex))
            {
                if (ex != null)
                {
                    throw new Exception($"Group {GetGroupAt(i)} at code {All[i]} ({i}) returned {ex}");
                }
                continue;
            }
            
            if (HandleCast(c, out ex))
            {
                if (ex != null)
                {
                    throw new Exception($"Group {GetGroupAt(i)} at code {All[i]} ({i}) returned {ex}");
                }
                continue;
            }
            
            if (HandleUnConditionalBr(c, out ex))
            {
                if (ex != null)
                {
                    throw new Exception($"Group {GetGroupAt(i)} at code {All[i]} ({i}) returned {ex}");
                }
                continue;
            }

            if (HandleMath(c, out ex))
            {
                if (ex != null)
                {
                    throw new Exception($"Group {GetGroupAt(i)} at code {All[i]} ({i}) returned {ex}");
                }
                continue;
            }

            if (HandleConditionalBr(c, out ex, stackTypes, Labels))
            {
                if (ex != null)
                {
                    throw new Exception($"Group {GetGroupAt(i)} at code {All[i]} ({i}) returned {ex}");
                }
                continue;
            }
        }

        foreach (var l in Labels.Values)
        {
            if (!l.IsAssigned)
            {
                throw new Exception($"Failed to assign a label {l}");
            }

            if (!l.HasProperJump)
            {
                throw new Exception($"Failed to give a label {l} a valid branch");
            }
        }

        return;
        
        bool HandleLabel(CodeInstruction c, out Exception exception)
        {
            exception = null;
            var labels = c.labels;
            if (labels != null && labels.Count > 0)
            {
                foreach (var l in labels)
                {
                    if (!Labels.TryGetValue(l, out var status))
                    {
                        status = new LabelStatus();
                    }

                    if (status.IsAssigned)
                    {
                        exception = new Exception($"Label {l} was already assigned at {status.Target}");
                        return true;
                    }
                    status.Target = c;
                    status.IsAssigned = true;
                    Labels[l] = status;
                }
            }

            return false;
        }
        
        bool HandleField(CodeInstruction c)
        {
            if (LoadFieldOpCodes.Contains(c.opcode))
            {
                var field = (FieldInfo)c.operand;
                var result = field.FieldType;
                if (c.opcode == OpCodes.Ldsflda || c.opcode == OpCodes.Ldflda)
                {
                    result = result.MakeByRefType();
                }
                stackTypes.Add(result);
                return true;
            }
            return false;
        }
        
        bool HandleArg(CodeInstruction c, out Exception exception)
        {
            exception = null;
            if (GetArgumentOpCodes.Contains(c.opcode))
            {
                int index;
                if (OpCodes.Ldarg_0 == c.opcode)
                {
                    index = 0;
                }
                else if (OpCodes.Ldarg_1 == c.opcode)
                {
                    index = 1;
                }
                else if (OpCodes.Ldarg_2 == c.opcode)
                {
                    index = 2;
                }
                else if (OpCodes.Ldarg_3 == c.opcode)
                {
                    index = 3;
                }
                else
                {
                    index = (int)c.operand;
                }

                if (index >= CurrentParameters.Count)
                {
                    exception = new Exception($"Tried finding argument at index {index}, but the method only has {CurrentParameters.Count} arguments!");
                    return true;
                }
                
                var param = CurrentParameters[index];

                if (OpCodes.Ldarga == c.opcode || OpCodes.Ldarga_S == c.opcode)
                {
                    param = param.MakeByRefType();
                }
                
                stackTypes.Add(param);
                return true;
            }
            return false;
        }

        bool HandleCall(CodeInstruction c, out Exception exception)
        {
            exception = null;
            if (StackCrawler.CallOpCodes.Contains(c.opcode))
            {
                exception = HandleTypesFromMethod(c, stackTypes);
                return true;
            }
            return false;
        }

        bool HandleBox(CodeInstruction c, out Exception exception)
        {
            exception = null;
            if (c.opcode == OpCodes.Box)
            {
                var type = (Type)c.operand;
                if (!stackTypes.Last().IsValueType || stackTypes.Last() != type)
                {
                    exception = new Exception(
                        $"Tried to box {stackTypes.Last()} into {type}, but it can't");
                    return true;
                }
                stackTypes.Remove(stackTypes.Last());
                stackTypes.Add(typeof(object));
                return true;
            }
            return false;
        }

        bool HandleUnbox(CodeInstruction c, out Exception exception)
        {
            exception = null;
            if (c.opcode == OpCodes.Unbox)
            {
                var type = (Type)c.operand;
                type = type.MakeByRefType();
                if (!stackTypes.Last().IsClass)
                {
                    exception = new Exception(
                        $"Tried to unbox {stackTypes.Last()} into {type}, but it can't");
                    return true;
                }
                stackTypes.Remove(stackTypes.Last());
                stackTypes.Add(type);
                return true;
            }
            return false;
        }

        bool HandleUnboxAny(CodeInstruction c, out Exception exception)
        {
            exception = null;
            if (c.opcode == OpCodes.Unbox_Any)
            {
                var type = (Type)c.operand;
                if (stackTypes.Last() != typeof(object))
                {
                    exception = new Exception(
                        $"Tried to unbox any {stackTypes.Last()} into {type}");
                    return true;
                }
                stackTypes.Remove(stackTypes.Last());
                stackTypes.Add(type);
                return true;
            }
            return false;
        }

        bool HandleCast(CodeInstruction c, out Exception exception)
        {
            exception = null;
            if (c.opcode == OpCodes.Castclass)
            {
                var cast = (Type)c.operand;
                var last = stackTypes.Last();
                if (!cast.IsAssignableFrom(last))
                {
                    exception = new Exception($"Tried to cast {last} into {cast}, but it can't");
                    return true;
                }
                stackTypes.Remove(last);
                stackTypes.Add(cast);
                return true;
            }
            return false;
        }

        bool HandleUnConditionalBr(CodeInstruction c, out Exception exception)
        {
            exception = null;
            bool flag = OpCodes.Br == c.opcode || OpCodes.Br_S == c.opcode;
            if (UnConditionalBrOpCodes.Contains(c.opcode) && !flag)
            {
                var t = stackTypes.Last();
                if (!(t.IsPrimitive || t.IsValueType || t.IsClass))
                {
                    exception = new  Exception($"Tried to have a conditional check, but the last type {stackTypes.Last()} which is not valid");
                    return true;
                }
                stackTypes.Remove(stackTypes.Last());
                flag = true;
            }
            if (flag)
            {
                if (c.operand is not Label label)
                {
                    exception = new Exception($"CodeInstruction Br had the operand {c.operand.GetType()} instead of an {typeof(Label)}");
                    return true;
                }

                if (!Labels.TryGetValue(label, out var state))
                {
                    state = new LabelStatus();
                }

                state.HasProperJump = true;
                Labels[label] = state;
            }
            
            return flag;
        }

        bool HandleMath(CodeInstruction c, out Exception exception)
        {
            exception = null;
            if (MathOpCodes.Contains(c.opcode))
            {
                var last = stackTypes.Last();
                var secondLast = stackTypes[stackTypes.Count - 2];
                if (EnsureLast2TypesMatch(out exception))
                {
                    stackTypes.Remove(last);
                    stackTypes.Remove(secondLast);
                    stackTypes.Add(last);
                }

                return true;
            }

            return false;
        }

        bool EnsureLast2TypesMatch(out Exception exception)
        {
            if (stackTypes.Count < 2)
            {
                exception = new Exception($"Not enough values on the stack. Stack count was {stackTypes.Count}");
                return false;
            }
            var last = stackTypes.Last();
            var secondLast = stackTypes[stackTypes.Count - 2];
            if (last != secondLast)
            {
                exception = new Exception($"Invalid types for math operation! {last} and {secondLast}");
                return false;
            }
            exception = null;
            return true;
        }

        bool HandleLocals(CodeInstruction c, out Exception exception)
        {
            exception = null;
            
            if (!(GetLocalOpCodes.Contains(c.opcode) || c.opcode == OpCodes.Ldloca || c.opcode == OpCodes.Ldloca_S || SetLocalOpCodes.Contains(c.opcode)))
            {
                return false;
            }

            LocalVariableInfo builder = null;
            if (c.operand is int i)
            {
                if(i < Locals.Count)
                    builder = Locals[i];
                else
                {
                    exception = new Exception($"Local was out of bounds at {i}, local count {Locals.Count}");
                    return true;
                }
            }
            else if (c.operand is LocalBuilder l)
            {
                builder = l;
            }
            else if (c.opcode == OpCodes.Ldloc_0 || c.opcode == OpCodes.Stloc_0)
            {
                builder = Locals[0];
            }
            else if (c.opcode == OpCodes.Ldloc_1 || c.opcode == OpCodes.Stloc_1)
            {
                builder = Locals[1];
            }
            else if (c.opcode == OpCodes.Ldloc_2 || c.opcode == OpCodes.Stloc_2)
            {
                builder = Locals[2];
            }
            else if (c.opcode == OpCodes.Ldloc_3 || c.opcode == OpCodes.Stloc_3)
            {
                builder = Locals[3];
            }
            
            if (builder == null)
            {
                exception = new Exception($"No local was found for {c.operand}");
                return true;
            }

            if (SetLocalOpCodes.Contains(c.opcode))
            {
                var last = stackTypes.Last();
                if (last != builder.LocalType)
                {
                    exception = new Exception($"Attempted to store {last} in a local of type {builder.LocalType}");
                    return true;
                }
                stackTypes.RemoveAt(stackTypes.Count - 1);
                return true;
            }

            if (c.opcode == OpCodes.Ldloca || c.opcode == OpCodes.Ldloca_S)
            {
                stackTypes.Add(builder.LocalType.MakeByRefType());
                return true;
            }
            
            if (GetLocalOpCodes.Contains(c.opcode))
            {
                stackTypes.Add(builder.LocalType);
                return true;
            }
            
            return false;
        }
    }

    private bool HandleConditionalBr(CodeInstruction c, out Exception exception, List<Type> stack, Dictionary<Label, LabelStatus> labels)
    {
        exception = null;
        if (ConditionalBrOpCodes.Contains(c.opcode))
        {
            if (stack.Count < 2)
            {
                exception = new Exception($"Not enough types on the stack to make the conditional check, there was {string.Join(", ", stack)}");
                return true;
            }
            
            if (c.operand is not Label label)
            {
                exception = new Exception($"CodeInstruction had the operand {c.operand.GetType()} instead of an {typeof(Label)}");
                return true;
            }

            if (!labels.TryGetValue(label, out var state))
            {
                state = new LabelStatus();
            }

            state.HasProperJump = true;
            labels[label] = state;
            
            stack.RemoveAt(stack.Count - 1);
            stack.RemoveAt(stack.Count - 1);
            // todo check if types are compatible, but there's a ton of checks pain
        }
        return false;
    }
            
    private Exception? HandleTypesFromMethod(CodeInstruction c, List<Type> stack)
    {
        var method = (MethodInfo)c.operand;
        var paramTypes = method.GetParameters();
        int expectedCount = paramTypes.Length + (method.IsStatic ? 0 : 1);

        if (stack.Count < expectedCount)
            return new Exception($"Stack of size {stack.Count} was too small for method call which required {expectedCount} parameters");
        
        if (!method.IsStatic && !method.IsConstructor)
        {
            var instanceType = stack[stack.Count - expectedCount];
            if (instanceType != method.DeclaringType)
                return new Exception(
                    $"Stack imbalanced! Expected instance of {method.DeclaringType}, found {instanceType}");
        }
        
        for (int i = 0; i < paramTypes.Length; i++)
        {
            var paramType = paramTypes[i].ParameterType;
            var stackType = stack[stack.Count - paramTypes.Length + i];
            if (stackType != paramType)
                return new Exception(
                    $"Stack imbalanced! Expected parameter {paramType}, found {stackType}");
        }
        
        for (int i = 0; i < expectedCount; i++)
            stack.RemoveAt(stack.Count - 1);
        
        if (method.ReturnType != typeof(void))
            stack.Add(method.ReturnType);
        return null;
    }
    
    private struct LabelStatus
    {
        public bool IsAssigned = false;
        public bool HasProperJump = false;
        public Label label;
        public CodeInstruction Target;
        public LabelStatus()
        {
        }

        public override string ToString()
        {
            return $"[{label.GetHashCode()}]";
        }
    }
    
    private struct Null{}
}