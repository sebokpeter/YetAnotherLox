using System.Diagnostics;

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

    public override bool Equals(object? obj)
    {
        if(obj is null)
        {
            return false;
        }

        if(obj is not Obj loxObj)
        {
            return false;
        }

        if(Type != loxObj.Type)
        {
            return false;
        }


        return Type switch
        {
            ObjType.Function => loxObj.AsFunction.Equals(this),
            ObjType.String => loxObj.AsString.Equals(this),
            ObjType.Native => loxObj.AsNative.Equals(this),
            ObjType.Closure => loxObj.AsClosure.Equals(this),
            ObjType.UpValue => throw new NotImplementedException(),
            ObjType.Class => loxObj.AsClass.Equals(this),
            ObjType.Instance => loxObj.AsInstance.Equals(this),
            _   => throw new UnreachableException()
        };
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
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
    internal required Func<int, LoxValue> Func { get; init; }

    internal ObjNativeFn() : base(ObjType.Native) { }

    public override string ToString() => $"<native fn {Name}>";
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

internal enum ObjType
{
    Function,
    String,
    Native,
    Closure,
    UpValue,
    Class,
    Instance
}