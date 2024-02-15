using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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
    /// Treat this <see cref="Obj"/> as a string.
    /// </summary>
    public string AsString => (string)_obj;

    private Obj(string s)
    {
        _obj = s;
        Type = ObjType.String;
    } 

    /// <summary>
    /// Return a new <see cref="Obj"/>, where the lox runtime type is string, and the value is <paramref name="s"/>.
    /// </summary>
    /// <param name="s">The lox runtime value.</param>
    /// <returns></returns>
    public static Obj String(string s) => new(s);

    public override string ToString() => _obj.ToString()!;
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
    String,
}