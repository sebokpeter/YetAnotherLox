namespace LoxConsole.Interpreter;

internal class NativeFunction : ILoxCallable
{
    private readonly int _arity;
    private readonly Func<List<object>, object> _func;

    public int Arity => _arity;

    public NativeFunction(int arity, Func<List<object>, object> func)
    {
        _arity = arity;
        _func = func;
    }

    public object Call(Interpreter interpreter, List<object> arguments)
    {
        try
        {
            return _func(arguments);
        }
        catch (Return returnValue)
        {
            return returnValue.value!;
        }
    }
    public override string ToString() => $"<native fn {_func}>";

}
