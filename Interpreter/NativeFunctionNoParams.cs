
namespace LoxConsole.Interpreter;

internal class NativeFunctionNoParams : ILoxCallable
{
    private readonly Func<object> _func;

    public int Arity => 0;

    public NativeFunctionNoParams(Func<object> func)
    {
        _func = func;
    }

    public object Call(Interpreter interpreter, List<object> arguments) 
    {
        try
        {
            return _func();
        }
        catch (Return returnValue)
        {
            return returnValue.value!;
        }
    }

    public override string ToString() => $"<native fn {_func}>";
}