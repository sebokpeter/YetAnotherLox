using System.Diagnostics;
using System.Net.Sockets;

namespace LoxVM.Value;

/// <summary>
/// Wrapper around a dynamic lox object (e.g. string, function, etc.)
/// </summary>
internal abstract class Obj
{
    /// <summary>
    /// The lox runtime type.
    /// </summary>
    internal ObjType Type { get; init; }

    /// <summary>
    /// Treat this <see cref="Obj"/> as an <see cref="ObjString"/>.
    /// </summary>
    internal ObjString AsString => (ObjString)this;

    /// <summary>
    /// Treat this <see cref="Obj"/> as an <see cref="ObjFunction"/>.
    /// </summary>
    internal ObjFunction AsFunction => (ObjFunction)this;

    /// <summary>
    /// Treat this <see cref="Obj"/> as an <see cref="ObjNativeFn"/>.
    /// </summary>
    internal ObjNativeFn AsNative => (ObjNativeFn)this;

    /// <summary>
    /// Treat this <see cref="Obj"/> as an <see cref="ObjClosure"/>.
    /// </summary>
    internal ObjClosure AsClosure => (ObjClosure)this;

    /// <summary>
    /// Treat this <see cref="Obj"/> as an <see cref="ObjClass"/>.
    /// </summary>
    internal ObjClass AsClass => (ObjClass)this;

    /// <summary>
    /// Treat this <see cref="Obj"/> as an <see cref="ObjInstance"/>.
    /// </summary>
    internal ObjInstance AsInstance => (ObjInstance)this;

    /// <summary>
    /// Treat this <see cref="Obj"/> as an <see cref="ObjBoundMethod"/>.
    /// </summary>
    internal ObjBoundMethod AsBoundMethod => (ObjBoundMethod)this;

    internal Obj(ObjType type)
    {
        Type = type;
    }

    /// <summary>
    /// Check if this <see cref="Obj"/> wraps the given lox runtime type.
    /// </summary>
    /// <param name="type">The lox runtime type to be checked.</param>
    /// <returns>True if the lox runtime type of this <see cref="Obj"/> is <paramref name="type"/>, false otherwise.</returns>
    internal bool IsType(ObjType type) => Type == type;

    /// <summary>
    /// Create a new <see cref="ObjString"/> instance from a string.
    /// </summary>
    /// <param name="string"></param>
    /// <returns></returns>
    internal static ObjString Str(string @string) => new() { StringValue = @string };

    /// <summary>
    /// Create a new <see cref="ObjFunction"/> instance.
    /// </summary>
    /// <param name="arity">The arity of the function.</param>
    /// <param name="name">The name of the function.</param>
    /// <returns></returns>
    internal static ObjFunction Func(int arity, string name) => new() { Arity = arity, Name = name };

    /// <summary>
    /// Create a new <see cref="ObjClass"/> instance. The <see cref="ObjClass.Methods"/> dictionary will be initialized to empty.
    /// </summary>
    /// <param name="name">The name of the class.</param>
    /// <returns></returns>
    internal static ObjClass Class(string name) => new() { Name = name, Methods = [] };

    /// <summary>
    /// Create a new <see cref="ObjInstance"/> instance.
    /// </summary>
    /// <param name="class">The class of which this <see cref="ObjInstance"/> is an instance of.</param>
    /// <returns></returns>
    internal static ObjInstance Instance(ObjClass @class) => new() { ObjClass = @class, Fields = [] };

    /// <summary>
    /// Create a new <see cref="ObjBoundMethod"/> instance.
    /// </summary>
    /// <param name="receiver">The instance to which the <paramref name="method"/> is bound.</param>
    /// <param name="method">The method that is being bound to <paramref name="receiver"/>.</param>
    /// <returns></returns>
    internal static ObjBoundMethod BoundMethod(LoxValue receiver, ObjClosure method) => new() { Receiver = receiver, Method = method };

    public abstract override bool Equals(object? obj);

    public abstract override int GetHashCode();

    public abstract override string ToString();
}

/// <summary>
/// Wrapper around a lox string
/// </summary>
internal class ObjString : Obj
{
    internal required string StringValue { get; set; }

    internal ObjString() : base(ObjType.String) { }

    public override string ToString() => StringValue;

    public override bool Equals(object? obj)
    {
        if(obj is null)
        {
            return false;
        }

        if(obj is not ObjString str)
        {
            return false;
        }

        return StringValue == str.StringValue;
    }

    public override int GetHashCode() => StringValue.GetHashCode();
}

/// <summary>
/// Represents a lox function.
/// </summary>
internal class ObjFunction : Obj
{
    /// <summary>
    /// Number of arguments to this function.
    /// </summary>
    internal required int Arity { get; init; }
    /// <summary>
    /// Name of the function.
    /// </summary>
    internal required string Name { get; init; }
    /// <summary>
    /// The code and constants associated with this function.
    /// </summary>
    internal Chunk.Chunk Chunk { get; init; }

    /// <summary>
    /// How many upvalues does this function use.
    /// </summary>
    internal int UpValueCount { get; set; }

    internal ObjFunction() : base(ObjType.Function)
    {
        Chunk = new();
    }

    /// <summary>
    /// Shortcut for creating the top-level function. The <see cref="Arity"/> will be 0, and the <see cref="Name"/> will be empty.
    /// </summary>
    /// <returns></returns>
    internal static ObjFunction TopLevel() => new() { Arity = 0, Name = "" };

    public override string ToString() => String.IsNullOrEmpty(Name) ? "<script>" : $"<fn {Name}>";

    public override bool Equals(object? obj)
    {
        if(obj is null)
        {
            return false;
        }

        if(obj is not ObjFunction objFunction)
        {
            return false;
        }

        return Arity == objFunction.Arity && Name == objFunction.Name;
    }

    public override int GetHashCode() => HashCode.Combine(Name, Arity);
}

/// <summary>
/// Represents a native lox function.
/// </summary>
internal class ObjNativeFn : Obj
{
    /// <summary>
    /// The number of arguments to this function.
    /// </summary>
    internal required int Arity { get; init; }

    /// <summary>
    /// The name of this function.
    /// </summary>
    internal required string Name { get; init; }

    /// <summary>
    /// A <see cref="Func{int, LoxValue}"/> object, that will be invoked when this native function is called.
    /// </summary>
    internal required Func<int, LoxValue> Function { get; init; }

    internal ObjNativeFn() : base(ObjType.Native) { }

    public override string ToString() => $"<native fn {Name}>";

    public override bool Equals(object? obj)
    {
        if(obj is null)
        {
            return false;
        }

        if(obj is not ObjNativeFn nativeFn)
        {
            return false;
        }

        return Arity == nativeFn.Arity && Name == nativeFn.Name;
    }

    public override int GetHashCode() => HashCode.Combine(Arity, Name);
}


/// <summary>
/// Represents a lox closure.
/// </summary>
internal class ObjClosure : Obj
{
    /// <summary>
    /// The function in this closure.
    /// </summary>
    internal required ObjFunction Function { get; init; }

    /// <summary>
    /// The upvalues that are used in this closure.
    /// </summary>
    internal required List<ObjUpValue> UpValues { get; init; }

    internal ObjClosure() : base(ObjType.Closure) { }

    public override string ToString() => Function.ToString();

    public override bool Equals(object? obj)
    {
        if(obj is null)
        {
            return false;
        }

        if(obj is not ObjClosure objClosure)
        {
            return false;
        }

        return Function.Equals(objClosure.Function);
    }

    public override int GetHashCode() => Function.GetHashCode();
}

/// <summary>
/// Represents a local variable in an enclosing function.
/// </summary>
internal class ObjUpValue : Obj
{
    /// <summary>
    /// A reference to the variable.
    /// </summary>
    internal required LoxValue LoxValue { get; set; }
    internal ObjUpValue? Next { get; set; }
    internal LoxValue Closed { get; set; }

    internal ObjUpValue() : base(ObjType.UpValue)
    {
        Next = null;
        Closed = LoxValue.Nil();
    }

    public override string ToString() => $"<upvalue {LoxValue}>";

    public override bool Equals(object? obj)
    {
        if(obj is null)
        {
            return false;
        }

        if(obj is not ObjUpValue upValue)
        {
            return false;
        }

        return LoxValue.Equals(upValue.LoxValue) && Closed.Equals(upValue.Closed) && (Next?.Equals(upValue.Next) ?? upValue.Next is null);
    }

    public override int GetHashCode() => HashCode.Combine(LoxValue, Next, Closed);
}

/// <summary>
/// Represents a lox class.
/// </summary>
internal class ObjClass : Obj
{
    /// <summary>
    /// The name of the class.
    /// </summary>
    internal required string Name { get; init; }

    internal required Dictionary<string, LoxValue> Methods { get; init; }

    public override string ToString() => $"<class {Name}>";

    internal ObjClass() : base(ObjType.Class) { }

    public override bool Equals(object? obj)
    {
        if(obj is null)
        {
            return false;
        }

        if(obj is not ObjClass @class)
        {
            return false;
        }

        return Name == @class.Name;
    }

    public override int GetHashCode() => Name.GetHashCode();
}

/// <summary>
/// Represents a lox class instance.
/// </summary>
internal class ObjInstance : Obj
{
    /// <summary>
    /// The lox class of which this <see cref="ObjInstance"/> is an instance of.
    /// </summary>
    internal required ObjClass ObjClass { get; init; }

    /// <summary>
    /// The fields of this instance.
    /// </summary>
    internal required Dictionary<string, LoxValue> Fields { get; init; }

    internal ObjInstance() : base(ObjType.Instance) { }

    public override string ToString() => $"<{ObjClass.Name} instance>";

    public override bool Equals(object? obj)
    {
        if(obj is null)
        {
            return false;
        }

        if(obj is not ObjInstance objInstance)
        {
            return false;
        }

        return ObjClass.Equals(objInstance.ObjClass) && Fields.SequenceEqual(objInstance.Fields);
    }

    public override int GetHashCode() => HashCode.Combine(ObjClass, Fields);
}

internal class ObjBoundMethod : Obj
{
    internal required LoxValue Receiver { get; init; }
    internal required ObjClosure Method { get; init; }

    internal ObjBoundMethod() : base(ObjType.BoundMethod) { }

    public override string ToString() => Method.Function.ToString();

    public override bool Equals(object? obj)
    {
        if(obj is null)
        {
            return false;
        }

        if(obj is not ObjBoundMethod boundMethod)
        {
            return false;
        }

        return Receiver.Equals(boundMethod.Receiver) && Method.Equals(boundMethod.Method);
    }

    public override int GetHashCode() => HashCode.Combine(Receiver, Method);
}

internal enum ObjType
{
    Function,
    String,
    Native,
    Closure,
    UpValue,
    Class,
    Instance,
    BoundMethod
}