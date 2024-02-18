using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;

namespace LoxVM.Value;

/// <summary>
/// Represents a Lox value at runtime.
/// </summary>
internal readonly struct LoxValue
{
    private const long TRUE_MASK = 0x000000000000000F; // Bit mask for boolean true value

    private static readonly LoxValue _nilValue = new() { Type = ValueType.Nil };  // Cache a 'nil' value, since we only need one

    private readonly long _internalValue;

    private readonly Obj _internalObject;

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
    public bool IsString => Type == ValueType.Obj && _internalObject.Type == ObjType.String;

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
        else if(o is ObjFunction obj)
        {
            return new LoxValue(Obj.Function(obj));
        }

        throw new NotImplementedException();   
    }

    public override readonly string ToString()
    {
        return Type switch
        {
            ValueType.Bool      => AsBool.ToString(),
            ValueType.Nil       => "nil",
            ValueType.Number    => AsNumber.ToString(),
            ValueType.Obj       => _internalObject!.ToString()!,
            _ => throw new UnreachableException()
        };
    }
}

/// <summary>
/// Wrapper around a heap allocated object (e.g. string)
/// </summary>
internal readonly struct Obj
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
    /// Returns true, if the lox runtime of this <see cref="Obj"/> is function.
    /// </summary>
    public bool IsFunction => Type == ObjType.Function;

    /// <summary>
    /// Treat this <see cref="Obj"/> as a string.
    /// </summary>
    public string AsString => (string)_obj;

    /// <summary>
    /// Treat this <see cref="Obj"/> as a <see cref="ObjFunction"/>.
    /// </summary>
    public ObjFunction AsFunction => (ObjFunction)_obj;

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

    public override string ToString() => _obj.ToString()!;
}

internal readonly struct ObjFunction
{
    internal int Arity { get; init; }
    internal string Name { get; init; }
    internal Chunk.Chunk Chunk { get; init; }

    public ObjFunction(int arity, string name)
    {
        Arity = arity;
        Name = name;
        Chunk = new();
    }

    public static ObjFunction TopLevel() => new(0, "");

    public override string ToString() => String.IsNullOrEmpty(Name) ? "<script>" : $"<fn {Name}>";
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
}