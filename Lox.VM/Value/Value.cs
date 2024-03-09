using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection.Metadata;

namespace LoxVM.Value;

/// <summary>
/// Represents a Lox value at runtime.
/// </summary>
internal class LoxValue
{
    private const long TRUE_MASK = 0x000000000000000F; // Bit mask for boolean true value

    private static readonly LoxValue _nilValue = new();  // Cache a 'nil' value, since we only need one

    private long internalValue;

    private Obj? internalObject;

    internal ValueType Type { get; init; }

    /// <summary>
    /// Returns true if the lox runtime type of this <see cref="LoxValue"/> is <see cref="null"/>.
    /// </summary>
    internal bool IsNil => Type == ValueType.Nil;

    /// <summary>
    /// Returns true if the lox runtime type of this <see cref="LoxValue"/> is <see cref="double"/>.
    /// </summary>
    internal bool IsNumber => Type == ValueType.Number;

    /// <summary>
    /// Returns true if the lox runtime type of this <see cref="LoxValue"/> is <see cref="bool"/>.
    /// </summary>
    internal bool IsBool => Type == ValueType.Bool;

    /// <summary>
    /// Returns true if the lox runtime type of this <see cref="LoxValue"/> is an object .
    /// </summary>
    [MemberNotNullWhen(true, nameof(internalObject))]
    internal bool IsObj => Type == ValueType.Obj;

    /// <summary>
    /// Returns true if the lox runtime type is string.
    /// </summary>
    internal bool IsString => Type == ValueType.Obj && internalObject!.Type == ObjType.String;

    /// <summary>
    /// Treat the value in this <see cref="LoxValue"/> as a <see cref="double"/>.
    /// </summary>
    internal double AsNumber => BitConverter.Int64BitsToDouble(internalValue);

    /// <summary>
    /// Treat the value in this <see cref="LoxValue"/> as a <see cref="bool"/>.
    /// </summary>
    internal bool AsBool => (internalValue & TRUE_MASK) == TRUE_MASK;

    /// <summary>
    /// Treat the value in this <see cref="LoxValue"/> as a <see cref="Obj"/>.
    /// </summary>
    internal Obj AsObj => internalObject!;

    private LoxValue(long val, ValueType type)
    {
        internalValue = val;
        Type = type;
    }

    private LoxValue()
    {
        internalObject = null;
        Type = ValueType.Nil;
    }

    private LoxValue(Obj o, ValueType type)
    {
        internalObject = o;
        Type = type;
    }

    private LoxValue(double d)
    {
        internalValue = BitConverter.DoubleToInt64Bits(d);
        Type = ValueType.Number;
    }

    private LoxValue(bool b)
    {
        if (b)
        {
            internalValue = TRUE_MASK;
        }

        Type = ValueType.Bool;
    }

    private LoxValue(Obj o)
    {
        internalObject = o;
        Type = ValueType.Obj;
    }

    /// <summary>
    /// Return a <see cref="LoxValue"/> where the lox runtime value is 'nil'.
    /// </summary>
    /// <returns></returns>
    internal static LoxValue Nil() => _nilValue;

    /// <summary>
    /// Return a <see cref="LoxValue"/> where the lox runtime type is 'number', and the value is <paramref name="d"/>.
    /// </summary>
    /// <param name="d">The lox runtime value.</param>
    /// <returns></returns>
    internal static LoxValue Number(double d) => new(d);

    /// <summary>
    /// Return a <see cref="LoxValue"/>, where the lox runtime type is 'bool', and the value is <paramref name="b"/>.
    /// </summary>
    /// <param name="b">The lox runtime value.</param>
    /// <returns></returns>
    internal static LoxValue Bool(bool b) => new(b);

    /// <summary>
    /// Return a <see cref="LoxValue"/>, where the lox runtime type is 'obj' (e.g. string), and the value is <paramref name="o"/>.
    /// </summary>
    /// <param name="o">The runtime value.</param>
    /// <returns></returns>
    internal static LoxValue Object(object o)
    {
        if (o is string s)
        {
            return new LoxValue(new ObjString() { StringValue = s });
        }
        else if (o is Obj obj)
        {
            return new LoxValue(obj, ValueType.Obj);
        }

        throw new NotImplementedException();
    }

    /// <summary>
    /// Create a copy of <paramref name="other"/>.
    /// </summary>
    /// <param name="other">Another <see cref="LoxValue"/>, which will be copied.</param>
    /// <returns></returns>
    internal static LoxValue FromLoxValue(LoxValue other)
    {
        if (other.IsObj)
        {
            return new(other.internalObject.Copy());
        }
        else
        {
            return new(other.internalValue, other.Type);
        }
    }

    /// <summary>
    /// Copy the value of <paramref name="other"/> into this <see cref="LoxValue"/>
    /// </summary>
    /// <param name="other">The <see cref="LoxValue"/> whose value will be copied.</param>
    internal void Copy(LoxValue other)
    {
        if (other.IsObj)
        {
            internalObject = other.internalObject;
        }
        else
        {
            internalValue = other.internalValue;
        }
    }

    public override string ToString()
    {
        return Type switch
        {
            ValueType.Bool => AsBool.ToString(),
            ValueType.Nil => "nil",
            ValueType.Number => AsNumber.ToString(),
            ValueType.Obj => internalObject!.ToString()!,
            _ => throw new UnreachableException()
        };
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is not LoxValue loxValue)
        {
            return false;
        }

        if (loxValue.Type != Type)
        {
            return false;
        }

        return Type switch
        {
            ValueType.Obj => AsObj.Equals(loxValue.AsObj),
            _ => internalValue == loxValue.internalValue,
        };
    }

    public override int GetHashCode() => Type == ValueType.Obj ? internalObject!.GetHashCode() : internalValue.GetHashCode();
}


internal enum ValueType
{
    Bool,
    Nil,
    Number,
    Obj
}