

namespace Lox.Interpreter;

/// <summary>
/// Represents a non-static class (one that can be instantiated).
/// A non-static class may contain static and non-static methods (incl. a constructor), and properties (set in the constructor using the 'this' keyword)
/// </summary>
internal class LoxNonStaticClass : LoxClass, ILoxCallable
{
    private readonly LoxNonStaticClass? _superclass;

    public int Arity
    {
        get
        {
            LoxFunction? initializer = FindMethod("init");
            if(initializer is null) 
            {
                return 0;
            }
            return initializer.Arity;
        }
    }

    public LoxNonStaticClass(string lexeme, LoxNonStaticClass? superclass, Dictionary<string, LoxFunction> methods) : base(lexeme, methods)
    {
        _superclass = superclass;
    }

    public override string ToString() => $"<class {Name}{InterpreterUtils.GetVariableString(Arity)}>";

    public object? Call(Interpreter interpreter, List<object> arguments)
    {
        LoxInstance instance = new(this);
        LoxFunction? initializer = FindMethod("init");
        initializer?.Bind(instance).Call(interpreter, arguments);

        return instance;
    }

    internal override LoxFunction? FindMethod(string name)
    {
        if (_methods.TryGetValue(name, out LoxFunction? method))
        {
            return method;
        }

        if(_superclass is not null)
        {
            return _superclass.FindMethod(name);
        }

        return null;
    }
}
