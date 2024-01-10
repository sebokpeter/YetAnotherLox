namespace Lox.Interpreter;

internal class LoxInstance
{
    private readonly LoxNonStaticClass _loxClass;
    private readonly Dictionary<string, object> _fields = [];

    internal LoxInstance(LoxNonStaticClass loxClass)
    {
        _loxClass = loxClass;
    }

    internal object? Get(Token name)
    {
        if(_fields.TryGetValue(name.Lexeme, out object? val)) 
        {
            return val;
        }

        LoxFunction? method = _loxClass.FindMethod(name.Lexeme);
        if(method is not null)
        {
            if(method.IsStatic)
            {
                throw new RuntimeException(name, $"Static method '{name.Lexeme}' cannot be called on an instance.");
            }

            return method.Bind(this);
        }

        throw new RuntimeException(name, $"Undefined property: {name.Lexeme}.");

    }

    internal void Set(Token name, object value)
    {
        _fields[name.Lexeme] = value;
    }

    public override string ToString() => $"<{_loxClass.Name} instance>";
}
