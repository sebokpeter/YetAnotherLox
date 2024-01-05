using System.Text;

namespace LoxConsole.Interpreter;

internal class NativeFunction : ILoxCallable
{
    private readonly int _arity;
    private readonly string _name;
    private readonly Func<List<object>, object> _func;


    public int Arity => _arity;

    public NativeFunction(int arity, string name, Func<List<object>, object> func)
    {
        _arity = arity;
        _func = func;
        _name = name;
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
    public override string ToString() => $"<native fn {_name} {GetArgsList()} >";

    private object GetArgsList()
    {
        StringBuilder argBuilder = new();
        argBuilder.Append('(');
        argBuilder.Append(String.Join(", ", Enumerable.Repeat("var", _arity)));
        argBuilder.Append(')');
        return argBuilder.ToString();
    }
}
