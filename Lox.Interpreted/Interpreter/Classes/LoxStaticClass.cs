namespace Lox.Interpreter;

/// <summary>
/// Represents a static class.
/// At runtime a static class is essentially just a collection of static methods.
/// </summary>
class LoxStaticClass : LoxClass
{
    public LoxStaticClass(string name, Dictionary<string, LoxFunction> methods) : base(name, methods)
    {
    }

    internal override LoxFunction? FindMethod(string name)
    {
        if(_methods.TryGetValue(name, out LoxFunction? method))
        {
            return method;
        }

        return null;
    }

    public override string ToString() => $"<static class {Name}>";
}