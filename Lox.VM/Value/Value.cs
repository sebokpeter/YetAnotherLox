namespace LoxVM.Value;

/// <summary>
/// Represents a Lox value at runtime.
/// </summary>
internal readonly struct LoxValue
{
    /// <summary>
    /// The runtime type.
    /// </summary>
    public ValueType Type { get; init; }
    
    /// <summary>
    /// The runtime value.
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// Returns true if the runtime type of this <see cref="LoxValue"/> is <see cref="null"/>.
    /// </summary>
    public bool IsNil => Type == ValueType.Nil;

    /// <summary>
    /// Returns true if the runtime type of this <see cref="LoxValue"/> is <see cref="double"/>.
    /// </summary>
    public bool IsNumber => Type == ValueType.Number;

    /// <summary>
    /// Returns true if the runtime type of this <see cref="LoxValue"/> is <see cref="bool"/>.
    /// </summary>
    public bool IsBool => Type == ValueType.Bool;


    // TODO: should we check the type before casting or should that be the responsibility of the caller?

    /// <summary>
    /// Return this value as a <see cref="double"/>
    /// </summary>
    public double AsNumber => (double)Value!;

    /// <summary>
    /// Return this value as a <see cref="bool"/>
    /// </summary>
    public bool AsBool => (bool)Value!;

    public LoxValue(ValueType type, object? value)
    {
        Type = type;
        Value = value;
    }

    /// <summary>
    /// Create a new <see cref="LoxValue"/>, representing the runtime numeric type, with the value <paramref name="value"/>. 
    /// </summary>
    /// <param name="value">The numeric value.</param>
    /// <returns></returns>
    public static LoxValue CreateNumberValue(double value) => new(ValueType.Number, value);
    
    /// <summary>
    /// Create a new <see cref="LoxValue"/>, representing the runtime nil value.
    /// </summary>
    /// <returns></returns>
    public static LoxValue CreateNilValue() => new(ValueType.Nil, null);

    /// <summary>
    /// Create a new <see cref="LoxValue"/>, representing the runtime boolean type, with the value <paramref name="b"/>.
    /// </summary>
    /// <param name="b">The boolean value.</param>
    /// <returns></returns>
    public static LoxValue CreateBoolValue(bool b) => new(ValueType.Bool, b);

    public override readonly string ToString()
    {
        return $"{Value}";
    }
}

internal enum ValueType
{
    Bool,
    Nil,
    Number
}