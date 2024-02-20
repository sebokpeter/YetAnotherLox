using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Security.AccessControl;

namespace LoxVM.Value;

/// <summary>
/// Represents a Lox value at runtime.
/// </summary>
internal class LoxValue
{
    private const long TRUE_MASK = 0x000000000000000F; // Bit mask for boolean true value

    private static readonly LoxValue _nilValue = new();  // Cache a 'nil' value, since we only need one

    private readonly long _internalValue;

    private readonly Obj? _internalObject;

    public ValueType Type { internal get; init; }

    /// <summary>
    /// Returns true if the lox runtime type of this <see cref="LoxValue"/> is <see cref="null"/>.
    /// </summary>
    public bool IsNil => Type == ValueType.Nil;

    /// <summary>
    /// Returns true if the lox runtime type of this <see cref="LoxValue"/> is <see cref="double"/>.
    /// </summary>
    public bool IsNumber => Type == ValueType.Number;

    /// <summary>
    /// Returns true if the lox runtime type of this <see cref="LoxValue"/> is <see cref="bool"/>.
    /// </summary>
    public bool IsBool => Type == ValueType.Bool;

    /// <summary>
    /// Returns true if the lox runtime type of this <see cref="LoxValue"/> is an object .
    /// </summary>
    public bool IsObj => Type == ValueType.Obj;

    /// <summary>
    /// Returns true if the lox runtime type is string.
    /// </summary>
    public bool IsString => Type == ValueType.Obj && _internalObject!.Type == ObjType.String;

    /// <summary>
    /// Treat the value in this <see cref="LoxValue"/> as a <see cref="double"/>.
    /// </summary>
    public double AsNumber => BitConverter.Int64BitsToDouble(_internalValue);

    /// <summary>
    /// Treat the value in this <see cref="LoxValue"/> as a <see cref="bool"/>.
    /// </summary>
    public bool AsBool => (_internalValue & TRUE_MASK) == TRUE_MASK;

    /// <summary>
    /// Treat the value in this <see cref="LoxValue"/> as a <see cref="object"/>.
    /// </summary>
    public Obj AsObj => _internalObject!;

    private LoxValue()
    {
        _internalObject = null;
        Type = ValueType.Nil;
    }

    private LoxValue(Obj o, ValueType type)
    {
        _internalObject = o;
        Type = type;
    }

    private LoxValue(double d)
    {
        _internalValue = BitConverter.DoubleToInt64Bits(d);
        Type = ValueType.Number;
    }

    private LoxValue(bool b)
    {
        if(b)
        {
            _internalValue = TRUE_MASK;
        }

        Type = ValueType.Bool;
    }

    private LoxValue(Obj o)
    {
        _internalObject = o;
        Type = ValueType.Obj;
    }

    /// <summary>
    /// Return a <see cref="LoxValue"/> where the lox runtime value is 'nil'.
    /// </summary>
    /// <returns></returns>
    public static LoxValue Nil() => _nilValue;

    /// <summary>
    /// Return a <see cref="LoxValue"/> where the lox runtime type is 'number', and the value is <paramref name="d"/>.
    /// </summary>
    /// <param name="d">The lox runtime value.</param>
    /// <returns></returns>
    public static LoxValue Number(double d) => new(d);

    /// <summary>
    /// Return a <see cref="LoxValue"/>, where the lox runtime type is 'bool', and the value is <paramref name="b"/>.
    /// </summary>
    /// <param name="b">The lox runtime value.</param>
    /// <returns></returns>
    public static LoxValue Bool(bool b) => new(b);

    /// <summary>
    /// Return a <see cref="LoxValue"/>, where the lox runtime type is 'obj' (e.g. string), and the value is <paramref name="o"/>.
    /// </summary>
    /// <param name="o">The runtime value.</param>
    /// <returns></returns>
    public static LoxValue Object(object o)
    {
        if(o is string s)
        {
            return new LoxValue(Obj.String(s));
        }
        else if(o is ObjFunction objFn)
        {
            return new LoxValue(Obj.Function(objFn));
        }
        else if(o is ObjNativeFn objNativeFn)
        {
            return new LoxValue(Obj.Native(objNativeFn));
        }
        else if(o is ObjClosure closure) // TODO: clean this up
        {
            return new LoxValue(Obj.Closure(closure.Function), ValueType.Obj);
        }
        else if(o is Obj obj)
        {
            return new LoxValue(obj);
        }

        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return Type switch
        {
            ValueType.Bool => AsBool.ToString(),
            ValueType.Nil => "nil",
            ValueType.Number => AsNumber.ToString(),
            ValueType.Obj => _internalObject!.ToString()!,
            _ => throw new UnreachableException()
        };
    }

    public override bool Equals(object? obj)
    {
        if(obj is null)
        {
            return false;
        }

        if(obj is not LoxValue loxValue)
        {
            return false;
        }

        if(loxValue.Type != Type)
        {
            return false;
        }

        return Type switch
        {
            ValueType.Bool      => _internalValue == loxValue._internalValue,
            ValueType.Nil       => _internalValue == loxValue._internalValue,
            ValueType.Number    => _internalValue == loxValue._internalValue,
            ValueType.Obj       => AsObj.Equals(loxValue),
            _                   => throw new UnreachableException()
        };
    }

    public override int GetHashCode() => Type == ValueType.Obj? _internalObject!.GetHashCode() : _internalValue.GetHashCode();
}

/// <summary>
/// Wrapper around lox object (e.g. string, function, etc.)
/// </summary>
internal class Obj
{
    /// <summary>
    /// The lox runtime type.
    /// </summary>
    public ObjType Type { internal get; init; }

    private readonly object _obj;

    /// <summary>
    /// Returns true, if the lox runtime type of this <see cref="Obj"/> is string.
    /// </summary>
    public bool IsString => Type == ObjType.String;

    /// <summary>
    /// Returns true, if the lox runtime type of this <see cref="Obj"/> is function.
    /// </summary>
    public bool IsFunction => Type == ObjType.Function;

    /// <summary>
    /// Returns true, if the lox runtime type of this <see cref="Obj"/> is a native function.
    /// </summary>
    public bool IsNative => Type == ObjType.Native;

    /// <summary>
    /// Returns true, if the lox runtime type of this <see cref="Obj"/> is a closure.
    /// </summary>
    public bool IsClosure => Type == ObjType.Closure;

    /// <summary>
    /// Returns true, if the lox runtime type of this <see cref="Objs"/> is an upvalue.
    /// </summary>
    public bool IsUpValue => Type == ObjType.UpValue;

    /// <summary>
    /// Treat this <see cref="Obj"/> as a string.
    /// </summary>
    public string AsString => (string)_obj;

    /// <summary>
    /// Treat this <see cref="Obj"/> as a <see cref="ObjFunction"/>.
    /// </summary>
    public ObjFunction AsFunction => (ObjFunction)_obj;

    /// <summary>
    /// Treat this <see cref="Obj"/> as a <see cref="ObjNativeFn"/>.
    /// </summary>
    public ObjNativeFn AsNativeFn => (ObjNativeFn)_obj;

    /// <summary>
    /// Treat this <see cref="Obj"/> as a <see cref="ObjClosure"/>.
    /// </summary>
    public ObjClosure AsClosure => (ObjClosure)_obj;

    /// <summary>
    /// Treat this <see cref="Obj"/> as a <see cref="ObjUpValue"/>.
    /// </summary>
    public ObjUpValue AsUpValue => (ObjUpValue)_obj;

    private Obj(string s)
    {
        _obj = s;
        Type = ObjType.String;
    }

    private Obj(ObjFunction objFunction)
    {
        _obj = objFunction;
        Type = ObjType.Function;
    }

    private Obj(ObjNativeFn nativeFn)
    {
        _obj = nativeFn;
        Type = ObjType.Native;
    }

    private Obj(ObjClosure objClosure)
    {
        _obj = objClosure;
        Type = ObjType.Closure;
    }

    public Obj(ObjUpValue objUpValue)
    {
        _obj = objUpValue;
        Type = ObjType.UpValue;
    }

    /// <summary>
    /// Return a new <see cref="Obj"/>, where the lox runtime type is string, and the value is <paramref name="s"/>.
    /// </summary>
    /// <param name="s">The lox runtime value.</param>
    /// <returns></returns>
    public static Obj String(string s) => new(s);

    /// <summary>
    /// Return a new <see cref="Obj"/>, where the lox runtime type is function, and the value is <paramref name="objFunction"/>.
    /// </summary>
    /// <param name="objFunction">The lox runtime value, a lox function.</param>
    /// <returns></returns>
    public static Obj Function(ObjFunction objFunction) => new(objFunction);

    /// <summary>
    /// Return a new <see cref="Obj"/>, where the lox runtime type is a native function, and the value is <paramref name="native"/>.
    /// </summary>
    /// <param name="objFunction">The lox runtime value, a native lox function.</param>
    /// <returns></returns>
    public static Obj Native(ObjNativeFn native) => new(native);

    /// <summary>
    /// Return a new <see cref="Obj"/>, where the lox runtime value is a closure that wraps <paramref name="objFunction"/>.
    /// </summary>
    /// <param name="objFunction">The wrapped function.</param>
    /// <returns></returns>
    public static Obj Closure(ObjFunction objFunction)
    {
        ObjClosure objClosure = new() { Function = objFunction, UpValues = [] };
        return new(objClosure);
    }

    /// <summary>
    /// Return a new <see cref="Obj"/>, where the lox runtime value is an upvalue.
    /// </summary>
    /// <param name="objUpValue"></param>
    /// <returns></returns>
    public static Obj UpValue(ObjUpValue objUpValue) => new(objUpValue);
    public override string ToString() => _obj.ToString()!;

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

        if(loxObj.Type != Type)
        {
            return false;
        }

        return Type switch
        {
            ObjType.Function    => AsFunction.Name == loxObj.AsFunction.Name && AsFunction.Arity == loxObj.AsFunction.Arity,
            ObjType.String      => AsString == loxObj.AsString,
            ObjType.Native      => AsNativeFn.Name == loxObj.AsNativeFn.Name && AsNativeFn.Arity == loxObj.AsNativeFn.Arity,
            ObjType.Closure     => throw new NotImplementedException(),
            ObjType.UpValue     => throw new NotImplementedException(),
            _                   => throw new UnreachableException(),
        };
    }

    public override int GetHashCode() => _obj.GetHashCode();
}

internal class ObjFunction
{
    internal int Arity { get; init; }
    internal string Name { get; init; }
    internal Chunk.Chunk Chunk { get; init; }
    internal int UpValueCount { get; set; }

    private ObjFunction(int arity, string name)
    {
        Arity = arity;
        Name = name;
        Chunk = new();
    }

    /// <summary>
    /// Shortcut for creating the top-level function <see cref="ObjFunction.Arity"/> = 0, <see cref="ObjFunction.Name"/> = ""
    /// </summary>
    /// <returns></returns>
    internal static ObjFunction TopLevel() => new(0, "");

    /// <summary>
    /// Create a new <see cref="ObjFunction"/>, with the given arity and name.
    /// </summary>
    /// <param name="arity">The arity (number of parameters) of the function.</param>
    /// <param name="name">The name of the function.</param>
    /// <returns></returns>
    internal static ObjFunction Function(int arity, string name) => new(arity, name);
    public override string ToString() => String.IsNullOrEmpty(Name) ? "<script>" : $"<fn {Name}>";
}

internal class ObjNativeFn
{
    internal required int Arity { get; init; }
    internal required string Name { get; init; }
    internal required Func<int, LoxValue> Func { get; set; }

    public override string ToString() => $"<native fn {Name}>";
}

internal class ObjClosure
{
    internal required ObjFunction Function { get; set; }
    internal required List<ObjUpValue> UpValues { get; set; }

    public override string ToString() => Function.ToString();
}


internal class ObjUpValue
{
    internal required LoxValue LoxValue { get; set; }

    public override string ToString() => $"Upvalue - {LoxValue}";
}


internal enum ValueType
{
    Bool,
    Nil,
    Number,
    Obj
}

internal enum ObjType
{
    Function,
    String,
    Native,
    Closure,
    UpValue
}