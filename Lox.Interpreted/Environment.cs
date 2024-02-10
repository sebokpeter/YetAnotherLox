using Shared;
using Lox.Interpreter;

internal class Environment
{
    private readonly Environment? _enclosing;
    private readonly Dictionary<string, object> _values = [];

    internal Dictionary<string, object> Values => _values;
    internal Environment? Enclosing => _enclosing;

    public Environment()
    {
        _enclosing = null;
    }

    public Environment(Environment enclosing)
    {
        _enclosing = enclosing;
    }

    internal object GetAt(int distance, string name)
    {
        return Ancestor(distance).Values[name];
    }

    private Environment Ancestor(int distance)
    {
        Environment environment = this;
        for (int i = 0; i < distance; i++)
        {
            environment = environment._enclosing!;
        }

        return environment;
    }

    internal void Assign(Token name, object value)
    {
        if (Values.ContainsKey(name.Lexeme))
        {
            _values[name.Lexeme] = value;
            return;
        }

        if (_enclosing is not null)
        {
            _enclosing.Assign(name, value);
            return;
        }

        throw new RuntimeException(name, $"Undefined variable '{name.Lexeme}'.");
    }

    internal void Define(string name, object value)
    {
        _values[name] = value;
    }

    internal object Get(Token name)
    {
        if (_values.TryGetValue(name.Lexeme, out object? value))
        {
            return value;
        }

        if (_enclosing is not null)
        {
            return _enclosing.Get(name);
        }

        throw new RuntimeException(name, $"Undefined variable '{name.Lexeme}'.");
    }

    internal void AssignAt(int distance, Token name, object value)
    {
        Ancestor(distance).Values[name.Lexeme] = value;
    }

    public override string ToString()
    {
        string result = _values.ToString()!;
        if (_enclosing is not null)
        {
            result += " -> " + _enclosing.ToString();
        }
        
        return result;
    }
}