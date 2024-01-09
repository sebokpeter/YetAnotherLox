using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Generated;
using Lox.Interpreter;

internal class NativeFunction
{
    internal class Sleep() : ILoxCallable
    {
        public int Arity => 1;

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            int ms = InterpreterUtils.ToNum<int>(arguments[0]);
            Thread.Sleep(ms);

            return null!;
        }

        public override string ToString() => $"<native fn sleep{InterpreterUtils.GetVariableString(this)}>";
    }

    internal class Clock() : ILoxCallable
    {
        public int Arity => 0;

        public object Call(Interpreter interpreter, List<object> arguments) => DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond / 1000.0;
        public override string ToString() => "<native fn clock>";
    }

    internal class LoxRandom() : ILoxCallable
    {
        public int Arity => 1;

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            int max = InterpreterUtils.ToNum<int>(arguments[0]);
            return (double)Random.Shared.Next(max);
        }

        public override string ToString() => $"<native fn random{InterpreterUtils.GetVariableString(this)}>";
    }

    internal class Stringify : ILoxCallable
    {
        private readonly Func<object?, string> _strFunc;

        public int Arity => 1;

        internal Stringify(Func<object?, string> strFunc)
        {
            _strFunc = strFunc;
        }

        public object Call(Interpreter interpreter, List<object> arguments) => _strFunc(arguments[0]);

        public override string ToString() => $"<native fn stringify{InterpreterUtils.GetVariableString(this)}>";
    }

    internal class Num : ILoxCallable
    {
        public int Arity => 1;

        public object Call(Interpreter interpreter, List<object> arguments) => InterpreterUtils.ToNum<double>(arguments[0]);

        public override string ToString() => $"<native fn num{InterpreterUtils.GetVariableString(this)}>";
    }

    internal class Input : ILoxCallable
    {
        public int Arity => 0;

        public object Call(Interpreter interpreter, List<object> arguments) => Console.ReadLine()!;

        public override string ToString() => $"<native fn input>";
    }

    internal class ReadFile : ILoxCallable
    {
        public int Arity => 1;

        public object Call(Interpreter interpreter, List<object> arguments) => File.ReadAllText((string)arguments[0]);

        public override string ToString() => $"<native fn readFile{InterpreterUtils.GetVariableString(this)}>";
    }

    internal class Len : ILoxCallable
    {
        public int Arity => 1;

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            object arg = arguments[0];
            if(arg is LoxArray array)
            {
                return (double)array.Values.Count;
            }

            if(arg is string s)
            {
                return (double)s.Length;
            }

            throw new Exception("Can only get the length of arrays and strings.");
        }

        public override string ToString() => $"<native fn len{InterpreterUtils.GetVariableString(this)}>";
    }

    internal class Write : ILoxCallable
    {
        private readonly Func<object, string> _strFunc;

        public int Arity => 1;

        public Write(Func<object, string> strFunc)
        {
            _strFunc = strFunc;
        }

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            Console.Write(_strFunc(arguments[0]));
            return null!;
        }
        public override string ToString() => $"<native fn write{InterpreterUtils.GetVariableString(this)}>";
    }

    internal class Clear : ILoxCallable
    {
        public int Arity => 0;

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            Console.Clear();
            return null!;
        }

        public override string ToString() => $"<native fn clear>";
    }

    internal class Int : ILoxCallable
    {
        public int Arity => 1;

        public object Call(Interpreter interpreter, List<object> arguments) 
        {
            double val = InterpreterUtils.ToNum<double>(arguments[0]);
            return Math.Round(val); // The runtime representation is still a double, but this removes the fractional part.
        }

        public override string ToString() => $"<native fn int{InterpreterUtils.GetVariableString(this)}>";
    }
}