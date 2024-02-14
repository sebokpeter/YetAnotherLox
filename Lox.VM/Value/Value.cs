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