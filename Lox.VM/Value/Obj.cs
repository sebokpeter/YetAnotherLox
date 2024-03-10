using System.Diagnostics;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;

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

    /// <summary>
    /// Treat this <see cref="Obj"/> as an <see cref="ObjArray"/>.
    /// </summary>
    internal ObjArray AsArray => (ObjArray)this;

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
    /// <param name="isStatic">A flag indicating if the class is a static class </param>
    /// <returns></returns>
    internal static ObjClass Class(string name, bool isStatic) => new() { Name = name, IsStatic = isStatic, Methods = [] };

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

    /// <summary>
    /// Create a new <see cref="ObjArray"/> instance.
    /// </summary>
    /// <param name="initialValues">A <see cref="List{LoxValue}"/> containing the initial values in this array. Can be null. If it is null, it will be initialized to an empty list.</param>
    /// <returns></returns>
    internal static ObjArray Arr(List<LoxValue>? initialValues = null) => new() { Array = initialValues ?? [] };

    /// <summary>
    /// Create a copy of this <see cref="Obj"/>.
    /// Note: not yet implemented for all subclasses.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    internal abstract Obj Copy();

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

    internal override ObjString Copy() => new() { StringValue = this.StringValue };

    public override string ToString() => StringValue;

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is not ObjString str)
        {
            return false;
        }

        return StringValue == str.StringValue;
    }

    public override int GetHashCode() => StringValue.GetHashCode();
}


/// <summary>
/// Represents a callable lox object, such as a function, native function, or class.
/// </summary>
internal abstract class ObjCallable : Obj
{
    protected string nameAndArguments = string.Empty;

    public ObjCallable(ObjType type) : base(type) { }

    protected string GetNameAndArguments(string name, int arity)
    {
        if (!String.IsNullOrWhiteSpace(nameAndArguments))
        {
            return nameAndArguments;
        }

        // Most callables will never need to print their name, so we don't create the nameAndArguments string beforehand
        // But once we do, we can save it 
        nameAndArguments = GetArgumentString(name, arity);
        return nameAndArguments;
    }

    private static string GetArgumentString(string name, int arity) => $"{name}({String.Join(", ", Enumerable.Repeat("var", arity))})"; // Probably faster than instantiating a StringBuilder every time

    public abstract override bool Equals(object? obj);

    public abstract override int GetHashCode();

    public abstract override string ToString();
}

/// <summary>
/// Represents a lox function.
/// </summary>
internal class ObjFunction : ObjCallable
{
    /// <summary>
    /// The name of this function.
    /// </summary>
    internal required string Name { get; init; }

    /// <summary>
    /// The number of arguments to this function.
    /// </summary>
    internal required int Arity { get; init; }

    /// <summary>
    /// The code and constants associated with this function.
    /// </summary>
    internal Chunk.Chunk Chunk { get; init; }

    /// <summary>
    /// How many upvalues does this function use.
    /// </summary>
    internal int UpValueCount { get; set; }

    /// <summary>
    /// True for static methods
    /// </summary>
    internal bool IsStatic { get; set; }

    internal ObjFunction() : base(ObjType.Function)
    {
        Chunk = new();
    }

    /// <summary>
    /// Shortcut for creating the top-level function. The <see cref="Arity"/> will be 0, and the <see cref="Name"/> will be empty.
    /// </summary>
    /// <returns></returns>
    internal static ObjFunction TopLevel() => new() { Arity = 0, Name = "" };

    public override string ToString() => String.IsNullOrEmpty(Name) ? "<script>" : $"<fn {GetNameAndArguments(Name, Arity)}>";

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is not ObjFunction objFunction)
        {
            return false;
        }

        return Arity == objFunction.Arity && Name == objFunction.Name;
    }

    public override int GetHashCode() => HashCode.Combine(Name, Arity);

    internal override Obj Copy() => new ObjFunction() { Arity = Arity, Name = Name };
}

/// <summary>
/// Represents a native lox function.
/// </summary>
internal class ObjNativeFn : ObjCallable
{
    /// <summary>
    /// The name of this native function.
    /// </summary>
    internal required string Name { get; init; }

    /// <summary>
    /// The number of arguments to this native function.
    /// </summary>
    internal required int Arity { get; init; }

    /// <summary>
    /// A <see cref="Func{int, (LoxValue?, bool)}"/> object, that will be invoked when this native function is called.
    /// The return type of the <see cref="Func"/> is <see cref="(LoxValue?, bool)"/>. The <see cref="bool"/> value is used to indicate if the function has completed successfully. A <see langword="false"/> value means that there were runtime errors. In this case <see cref="LoxValue?"/> will be <see langword="null"/>.
    /// </summary>
    internal required Func<int, (LoxValue?, bool)> Function { get; init; }

    internal ObjNativeFn() : base(ObjType.Native) { }

    public override string ToString() => $"<native fn {GetNameAndArguments(Name, Arity)}>";

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is not ObjNativeFn nativeFn)
        {
            return false;
        }

        return Arity == nativeFn.Arity && Name == nativeFn.Name;
    }

    public override int GetHashCode() => HashCode.Combine(Name, Arity);

    internal override Obj Copy() => new ObjNativeFn() { Name = Name, Arity = Arity, Function = Function };
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
    internal required List<LoxValue> UpValues { get; init; }

    internal ObjClosure() : base(ObjType.Closure) { }

    public override string ToString() => Function.ToString();

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is not ObjClosure objClosure)
        {
            return false;
        }

        return Function.Equals(objClosure.Function) && UpValues == objClosure.UpValues;
    }

    public override int GetHashCode() => Function.GetHashCode();

    internal override Obj Copy() => new ObjClosure() { Function = Function, UpValues = UpValues };
}

/// <summary>
/// Represents a lox class.
/// </summary>
internal class ObjClass : ObjCallable
{
    /// <summary>
    /// The name of this class
    /// </summary>
    internal required string Name { get; init; }

    /// <summary>
    /// A dictionary holding the methods of this class. 
    /// The key is the method's name, and the value is a <see cref="LoxValue"/>, which should be an <see cref="ObjClosure"/>.
    /// Holds static and instance methods for this class.
    /// </summary>
    internal required Dictionary<string, LoxValue> Methods { get; init; }

    /// <summary>
    /// True if the class is a static class.
    /// Static classes may only contain static methods.
    /// </summary>
    internal bool IsStatic { get; init; }

    public override string ToString() => $"<class {GetClassNameAndArguments()}>";

    private string GetClassNameAndArguments()
    {
        int arity = 0;

        if (Methods.TryGetValue("init", out LoxValue? initMethod))
        {
            arity = initMethod.AsObj.AsBoundMethod.Method.Function.Arity;
        }

        return GetNameAndArguments(Name, arity);
    }

    internal ObjClass() : base(ObjType.Class) { }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is not ObjClass @class)
        {
            return false;
        }

        return Name == @class.Name;
    }

    public override int GetHashCode() => Name.GetHashCode();
    internal override Obj Copy()
    {
        throw new NotImplementedException();
    }
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
        if (obj is null)
        {
            return false;
        }

        if (obj is not ObjInstance objInstance)
        {
            return false;
        }

        return ObjClass.Equals(objInstance.ObjClass) && Fields.SequenceEqual(objInstance.Fields);
    }

    public override int GetHashCode() => HashCode.Combine(ObjClass, Fields);

    internal override Obj Copy()
    {
        throw new NotImplementedException();
    }
}

internal class ObjBoundMethod : Obj
{
    internal required LoxValue Receiver { get; init; }
    internal required ObjClosure Method { get; init; }

    internal ObjBoundMethod() : base(ObjType.BoundMethod) { }

    public override string ToString() => Method.Function.ToString();

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is not ObjBoundMethod boundMethod)
        {
            return false;
        }

        return this == boundMethod;
    }

    public override int GetHashCode() => HashCode.Combine(Receiver, Method);

    internal override Obj Copy()
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Represents a lox array. Uses a <see cref="List{LoxValue}"/> to store the elements.
/// </summary>
internal class ObjArray : Obj
{
    public ObjArray() : base(ObjType.Array) { }

    /// <summary>
    /// A <see cref="List{LoxValue}"/> that holds the underlying elements.
    /// </summary>
    internal required List<LoxValue> Array { get; init; }

    internal override ObjArray Copy()
    {
        List<LoxValue> newValues = Array.Select(LoxValue.FromLoxValue).ToList();
        return new ObjArray() { Array = newValues };
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is not ObjArray array)
        {
            return false;
        }

        return Array.SequenceEqual(array.Array);
    }

    public override int GetHashCode() => Array.GetHashCode();

    public override string ToString() => $"<array {Array.Count}>";
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
    BoundMethod,
    Array
}