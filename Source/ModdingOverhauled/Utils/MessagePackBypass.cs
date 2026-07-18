using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using MessagePack;

namespace ModdingOverhauled.Utils;

/// <summary>
/// This is required because Messagepack wants a reference to ReadOnlyMemory from mscorlib, which isn't present in said mscorlib.
/// Even using System.Memory's nugget package does not work
/// </summary>
public static class MessagePackBypass {
    static MessagePackBypass() {
        SerializeDelegate = GetDelegateForSerialize();
        DeserializeDelegate = GetDelegateForDeserialize();
        WriteRawDelDelegate = GetDelegateForWriterRaw();
        SerializeWithWriterDelegate = GetDelegateForSerializeWithWriter();
        DeserializeObjWithReaderDelegate = GetDelegateForDeserializeObjWithReader();
    }

    private static readonly SerializeObj SerializeDelegate;
    private static readonly DeserializeObj DeserializeDelegate;
    private static readonly WriteRawDel WriteRawDelDelegate;
    private static readonly SerializeWithWriter SerializeWithWriterDelegate;
    private static readonly DeserializeObjWithReader DeserializeObjWithReaderDelegate;
    public static void WriteRaw(ref MessagePackWriter writer, byte[] data) {
        WriteRawDelDelegate(ref writer, data);
    }

    public static void Serialize(Type type, ref MessagePackWriter writer, object obj,
        MessagePackSerializerOptions options) {
        SerializeWithWriterDelegate(type, ref writer, obj, options);
    }
    
    public static byte[] Serialize(Type type, object obj, MessagePackSerializerOptions options = null) {
        return SerializeDelegate(type, obj, options, CancellationToken.None);
    }
    
    public static byte[] Serialize<T>(T obj, MessagePackSerializerOptions options = null) {
        return SerializeDelegate(typeof(T), obj,  options, CancellationToken.None);
    }

    public static T Deserialize<T>(byte[] data, MessagePackSerializerOptions options = null) {
        return (T)DeserializeDelegate(typeof(T), data, options, CancellationToken.None);
    }

    public static T Deserialize<T>(ref MessagePackReader reader, MessagePackSerializerOptions options) {
        return (T)DeserializeObjWithReaderDelegate(typeof(T), ref reader, options);
    }
    
    public static object Deserialize(Type type, byte[] data, MessagePackSerializerOptions options = null) {
        return DeserializeDelegate(type, data, options, CancellationToken.None);
    }
    
    private delegate void SerializeWithWriter(Type type, ref MessagePackWriter writer, object data, MessagePackSerializerOptions options);
    
    private static SerializeWithWriter GetDelegateForSerializeWithWriter() {
        var serializeMethod = typeof(MessagePackSerializer).GetMethod(nameof(MessagePackSerializer.Serialize), 
            BindingFlags.Static | BindingFlags.Public,
            null, 
            [typeof(Type), typeof(MessagePackWriter).MakeByRefType(), typeof(object), typeof(MessagePackSerializerOptions)],
            null
        );
        if (serializeMethod == null) {
            throw new Exception("Fatal error while creating MessagePack bypass");
        }
        return (SerializeWithWriter)serializeMethod.CreateDelegate(typeof(SerializeWithWriter));
    }
    
    private delegate byte[] SerializeObj(Type type, object obj, MessagePackSerializerOptions options, CancellationToken token);

    private static SerializeObj GetDelegateForSerialize() {
        var serializeMethod = typeof(MessagePackSerializer).GetMethod(nameof(MessagePackSerializer.Serialize), 
            BindingFlags.Static | BindingFlags.Public,
            null, 
            [typeof(Type), typeof(object), typeof(MessagePackSerializerOptions), typeof(CancellationToken)],
            null
            );
        if (serializeMethod == null) {
            throw new Exception("Fatal error while creating MessagePack bypass");
        }
        return (SerializeObj)serializeMethod.CreateDelegate(typeof(SerializeObj));
    }
    
    private delegate void WriteRawDel(ref MessagePackWriter writer, byte[] data);
    private static WriteRawDel GetDelegateForWriterRaw() {
        var readOnlySpanType = Type.GetType("System.ReadOnlySpan`1")!.MakeGenericType(typeof(byte));
        
        var opImplicit = readOnlySpanType.GetMethod(
            "op_Implicit",
            BindingFlags.Static | BindingFlags.Public,
            null,
            [typeof(byte[])],
            null);

        if (opImplicit == null) {
            throw new Exception("Fatal error while creating MessagePack bypass");
        }
        
        var toCall = typeof(MessagePackWriter).GetMethod(nameof(MessagePackWriter.WriteRaw), 
            BindingFlags.Instance | BindingFlags.Public,
            null, 
            [readOnlySpanType],
            null
        );

        if (toCall == null) {
            throw new Exception("Fatal error while creating MessagePack bypass");
        }
        
        var method = new DynamicMethod(
            "Call" + nameof(MessagePackBypass) + "WriteRaw",
            typeof(void),
            [typeof(MessagePackWriter).MakeByRefType(), typeof(byte[])],
            true);

        var il = method.GetILGenerator();
        
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Call, opImplicit);
        il.Emit(OpCodes.Call, toCall);
        il.Emit(OpCodes.Ret);
        return (WriteRawDel)method.CreateDelegate(typeof(WriteRawDel));
    }

    private delegate object DeserializeObjWithReader(Type type, ref MessagePackReader reader, MessagePackSerializerOptions options);
    
    private static DeserializeObjWithReader GetDelegateForDeserializeObjWithReader() {
        var serializeMethod = typeof(MessagePackSerializer).GetMethod(nameof(MessagePackSerializer.Deserialize), 
            BindingFlags.Static | BindingFlags.Public,
            null, 
            [typeof(Type), typeof(MessagePackReader).MakeByRefType(), typeof(MessagePackSerializerOptions)],
            null
        );
        if (serializeMethod == null) {
            throw new Exception("Fatal error while creating MessagePack bypass");
        }
        return (DeserializeObjWithReader)serializeMethod.CreateDelegate(typeof(DeserializeObjWithReader));
    }
    
    private delegate object DeserializeObj(Type type, byte[] bytes, MessagePackSerializerOptions options, CancellationToken token);
    
    private static DeserializeObj GetDelegateForDeserialize() {
        var readOnlyMemoryType = Type.GetType("System.ReadOnlyMemory`1")!.MakeGenericType(typeof(byte));
        
        var opImplicit = readOnlyMemoryType.GetMethod(
            "op_Implicit",
            BindingFlags.Static | BindingFlags.Public,
            null,
            [typeof(byte[])],
            null);

        if (opImplicit == null) {
            throw new Exception("Fatal error while creating MessagePack bypass");
        }
        
        var deserializeMethod = typeof(MessagePackSerializer).GetMethod(nameof(MessagePackSerializer.Deserialize), 
            BindingFlags.Static | BindingFlags.Public,
            null, 
            [typeof(Type), readOnlyMemoryType, typeof(MessagePackSerializerOptions), typeof(CancellationToken)],
            null
        );

        if (deserializeMethod == null) {
            throw new Exception("Fatal error while creating MessagePack bypass");
        }
        
        var method = new DynamicMethod(
            "Call" + nameof(MessagePackBypass) + "Deserialize",
            typeof(object),
            [typeof(Type), typeof(byte[]), typeof(MessagePackSerializerOptions), typeof(CancellationToken)],
            true);

        var il = method.GetILGenerator();
        
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Call, opImplicit);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Ldarg_3);
        il.Emit(OpCodes.Call, deserializeMethod);
        il.Emit(OpCodes.Ret);
        return (DeserializeObj)method.CreateDelegate(typeof(DeserializeObj));
    }
}