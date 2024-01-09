namespace Lox.Interpreter;

/// <summary>
/// Base class for both static and non-static classes.
/// </summary>
internal abstract class LoxClass
{
    internal string Name {get; init; }
    protected readonly Dictionary<string, LoxFunction> _methods;

    public LoxClass(string name, Dictionary<string, LoxFunction> methods)
    {
        Name = name;
        _methods = methods;
    } 

    internal abstract LoxFunction? FindMethod(string name);
}