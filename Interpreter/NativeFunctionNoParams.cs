
namespace LoxConsole.Interpreter;

internal class NativeFunctionNoParams : ILoxCallable
{
    private readonly string _name;
    private readonly Func<object> _func;

    public int Arity => 0;

    public NativeFunctionNoParams(string name, Func<object> func)
    {
        _func = func;
        _name = name;
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

    public override string ToString() => $"<native fn {_name}>";
}