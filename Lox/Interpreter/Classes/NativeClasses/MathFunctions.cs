using Generated;
using static Lox.Interpreter.StaticNativeClasses;

namespace Lox.Interpreter;

/// <summary>
/// Collection of math-related <see cref="LoxFunction"/>s.
/// </summary>
internal class MathFunctions
{
    internal class Pow : StaticNativeFunction
    {
        public Pow() : base("pow", 2) {}

        public override object Call(Interpreter interpreter, List<object> arguments)
        {
            double a = InterpreterUtils.ToNum<double>(arguments[0]);
            double b = InterpreterUtils.ToNum<double>(arguments[1]);

            return Math.Pow(a, b);
        }
    }

    internal class Sqrt : StaticNativeFunction
    {
        public Sqrt() : base("sqrt", 1) {}

        public override object Call(Interpreter interpreter, List<object> arguments)
        {
            double a = Double(arguments[0]);

            return Math.Sqrt(a);
        }
    }

    internal class Sin : StaticNativeFunction
    {
        public Sin() : base("sin", 1) {}

        public override object Call(Interpreter interpreter, List<object> arguments)
        {
            double a = Double(arguments[0]);

            return Math.Sin(a);
        }
    }

    internal class Cos : StaticNativeFunction
    {
        public Cos() : base("cos", 1) {}

        public override object Call(Interpreter interpreter, List<object> arguments)
        {
            double a = Double(arguments[0]);

            return Math.Cos(a);
        }
    }

    internal class Pi : StaticNativeFunction
    {
        public Pi() : base("pi", 0) {}

        public override object Call(Interpreter interpreter, List<object> arguments) => Math.PI;
    }

    private static double Double(object o) => InterpreterUtils.ToNum<double>(o);
}
