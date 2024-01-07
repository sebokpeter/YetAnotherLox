namespace LoxConsole.Interpreter;

internal interface ILoxCallable 
{
    int Arity {get; }
    object Call(Interpreter interpreter, List<object> arguments);
}