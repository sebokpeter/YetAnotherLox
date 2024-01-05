

namespace LoxConsole.Interpreter;

internal class LoxClass : ILoxCallable
{
    private readonly string _name;
    private readonly LoxClass? _superclass;
    private readonly Dictionary<string, LoxFunction> _methods;

    internal string Name => _name;

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

    public LoxClass(string lexeme, LoxClass? superclass, Dictionary<string, LoxFunction> methods)
    {
        _name = lexeme;
        _superclass = superclass;
        _methods = methods;
    }

    public override string ToString() => $"<class {Name}>";

    public object Call(Interpreter interpreter, List<object> arguments)
    {
        LoxInstance instance = new(this);
        LoxFunction? initializer = FindMethod("init");
        initializer?.Bind(instance).Call(interpreter, arguments);

        return instance;
    }

    internal LoxFunction? FindMethod(string name)
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
