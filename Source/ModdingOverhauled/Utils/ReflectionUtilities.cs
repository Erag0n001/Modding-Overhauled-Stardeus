using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace ModdingOverhauled.Utils;

public static class ReflectionUtilities
{
    /// <summary>
    /// Creates a performant delegate to call a method
    /// </summary>
    public static Delegate CreateMethodCall(MethodInfo info)
    {
        var parameterTypes = info.GetParameters().Select(p => p.ParameterType).ToList();
        if (!info.IsStatic)
        {
            parameterTypes.Insert(0, info.DeclaringType!);
        }
        var method = new DynamicMethod(
            "Call" + info.Name,
            info.ReturnType,
            parameterTypes.ToArray(),
            true);

        var il = method.GetILGenerator();
        
        for (short i = 0; i < parameterTypes.Count; i++)
        {
            il.Emit(OpCodes.Ldarg, i);
        }

        if (info.IsVirtual)
        {
            il.Emit(OpCodes.Callvirt, info);
        }
        else
        {
            il.Emit(OpCodes.Call, info);
        }
        il.Emit(OpCodes.Ret);

        return method.CreateDelegate(Expression.GetDelegateType(parameterTypes.Concat(new[] { info.ReturnType }).ToArray()));
    }
    
    /// <summary>
    /// Creates a performant setter to set the value of a field
    /// </summary>
    /// <typeparam name="T">Type the field is stored on (Aka Argument 0, or "this")</typeparam>
    /// <typeparam name="TValue">Type of the field</typeparam>
    public static Func<T, TValue> CreateSetter<T, TValue>(FieldInfo fieldInfo)
    {
        var method = new DynamicMethod(
            "Get" + fieldInfo.Name,
            typeof(void),
            [typeof(T), typeof(TValue)],
            true);
        
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stfld, fieldInfo);
        il.Emit(OpCodes.Ret);
        
        return (Func<T, TValue>)method.CreateDelegate(typeof(Func<T, TValue>));
    }

    /// <summary>
    /// Creates a performant getter to grab the value of a field
    /// </summary>
    /// <typeparam name="T">Type the property is stored on (Aka Argument 0, or "this")</typeparam>
    /// <typeparam name="TValue">Return value</typeparam>
    public static Func<T, TValue> CreateGetter<T, TValue>(FieldInfo fieldInfo)
    {
        var method = new DynamicMethod(
            "Get" + fieldInfo.Name,
            typeof(TValue),
            [typeof(T)],
            true);
        
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, fieldInfo);
        il.Emit(OpCodes.Ret);
        
        return (Func<T, TValue>)method.CreateDelegate(typeof(Func<T, TValue>));
    }
    
    /// <summary>
    /// Creates a performant getter to grab the value of a property
    /// </summary>
    /// <typeparam name="T">Type the property is stored on (Aka Argument 0, or "this")</typeparam>
    /// <typeparam name="TValue">Return value</typeparam>
    public static Func<T, TValue> CreateGetter<T, TValue>(PropertyInfo propertyInfo)
    {
        if (!propertyInfo.CanRead)
        {
            throw new Exception($"{propertyInfo.Name} does not have a getter.");
        }
        var method = new DynamicMethod(
            "Get" + propertyInfo.Name,
            typeof(TValue),
            [typeof(T)],
            true);
        
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, propertyInfo.GetMethod);
        il.Emit(OpCodes.Ret);
        
        return (Func<T, TValue>)method.CreateDelegate(typeof(Func<T, TValue>));
    }
    
    /// <summary>
    /// Creates a performant getter to grab the value of a property
    /// </summary>
    /// <typeparam name="T">Type the property is stored on (Aka Argument 0, or "this")</typeparam>
    /// <typeparam name="TValue">Type of the property</typeparam>
    public static Func<T, TValue> CreateSetter<T, TValue>(PropertyInfo propertyInfo)
    {
        if (!propertyInfo.CanWrite)
        {
            throw new Exception($"{propertyInfo.Name} does not have a setter.");
        }
        var method = new DynamicMethod(
            "Get" + propertyInfo.Name,
            typeof(void),
            [typeof(T), typeof(TValue)],
            true);
        
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Call, propertyInfo.SetMethod);
        il.Emit(OpCodes.Ret);
        
        return (Func<T, TValue>)method.CreateDelegate(typeof(Func<T, TValue>));
    }
}