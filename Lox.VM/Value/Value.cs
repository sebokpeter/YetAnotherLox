using System.Diagnostics;

namespace LoxVM.Value;

/// <summary>
/// Represents a Lox value at runtime.
/// </summary>
internal readonly struct LoxValue
{
    private const long TRUE_MASK = 0x000000000000000F; // Bit mask for boolean true value

    private static readonly LoxValue _nilValue = new() { Type = ValueType.Nil };  // Cache a 'nil' value, since we only need one

    private readonly long _internalValue;

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
    /// Treat the value in this <see cref="LoxValue"/> as a <see cref="double"/>.
    /// </summary>
    public double AsNumber => BitConverter.Int64BitsToDouble(_internalValue);

    /// <summary>
    /// Treat the value in this <see cref="LoxValue"/> as a <see cref="bool"/>.
    /// </summary>
    public bool AsBool => (_internalValue & TRUE_MASK) == TRUE_MASK;
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
    /// Return a <see cref="LoxValue"/> where the lox runtime type is 'bool', and the value is <paramref name="b"/>.
    /// </summary>
    /// <param name="b">The lox runtime value.</param>
    /// <returns></returns>
    public static LoxValue Bool(bool b) => new(b);


    public override readonly string ToString()
    {
        return Type switch
        {
            ValueType.Bool => AsBool.ToString(),
            ValueType.Nil => "nil",
            ValueType.Number => AsNumber.ToString(),
            _ => throw new UnreachableException()
        };
    }
}

internal enum ValueType
{
    Bool,
    Nil,
    Number
}