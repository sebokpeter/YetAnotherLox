
using Generated;

namespace Lox.Interpreter;


/// <summary>
/// Collection of <see cref="LoxStaticClass"/> implementations, that can be added to interpreter, to make them available during runtime.
/// </summary>
internal class StaticNativeClasses
{
    /// <summary>
    /// Abstract base class for static native functions.
    /// </summary>
    internal abstract class StaticNativeFunction : LoxFunction
    {
        private readonly int _arity;
        private readonly string _name;

        public override int Arity => _arity;
        public override bool IsStatic => true;


        // We set the base fields to null
        // It should be fine, since we override the methods and properties
        // TODO: try to find a better solution (different runtime representation for different methods?)

        public StaticNativeFunction(string name, int arity) : base(default!, default!, false)
        {
            _arity = arity;
            _name = name;
        }

        public abstract override object Call(Interpreter interpreter, List<object> arguments);

        public override string ToString() => GetToString(_name, this);
    }

    internal class LoxMath : LoxStaticClass
    {
        public LoxMath() : base("Math",
            new()
            {
                {"pow", new MathFunctions.Pow()},
                {"sqrt", new MathFunctions.Sqrt()},
                {"sin", new MathFunctions.Sin()},
                {"cos", new MathFunctions.Cos()},
                {"pi", new MathFunctions.Pi()}
            }
        )
        { }
    }


    private static string GetToString(string name, LoxFunction function) => $"<native fn {name}{InterpreterUtils.GetVariableString(function)}>";

}