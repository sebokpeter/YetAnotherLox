
using System.Runtime.CompilerServices;
using Generated;

namespace LoxConsole.Interpreter;

internal class LoxFunction : ILoxCallable
{
    private readonly Stmt.Function _declaration;
    private readonly Environment _closure;
    private readonly bool _isInitializer;

    public int Arity => _declaration.Params.Count; 
    public bool IsStatic => _declaration.IsStatic;

    public LoxFunction(Stmt.Function declaration, Environment closure, bool isInitializer)
    {
        _declaration = declaration;
        _closure = closure;
        _isInitializer = isInitializer;
    }

    public object Call(Interpreter interpreter, List<object> arguments)
    {
        Environment environment = new(_closure);
        for(int i = 0; i < _declaration.Params.Count; i++) 
        {
            environment.Define(_declaration.Params[i].Lexeme, arguments[i]);
        }   

        try
        {
            interpreter.ExecuteBlock(_declaration.Body, environment);
        }
        catch (Return returnValue)
        {
            if(_isInitializer)
            {
                return _closure.GetAt(0, "this");
            }
            return returnValue.value!;
        }

        if(_isInitializer)
        {
            return _closure.GetAt(0, "this");
        }

        return null!;
    }

    internal LoxFunction Bind(LoxInstance loxInstance)
    {
        Environment environment = new(_closure);
        environment.Define("this", loxInstance);
        return new LoxFunction(_declaration, environment, _isInitializer);  
    }

    public override string ToString() => $"<fn {_declaration.Name.Lexeme}>";
}