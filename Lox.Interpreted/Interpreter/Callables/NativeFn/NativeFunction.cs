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

        public override string ToString() => GetToString("sleep", Arity);
    }

    internal class Clock() : ILoxCallable
    {
        public int Arity => 0;

        public object Call(Interpreter interpreter, List<object> arguments) => DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond / 1000.0;
        public override string ToString() => GetToString("clock", Arity);
    }

    internal class LoxRandom() : ILoxCallable
    {
        public int Arity => 1;

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            int max = InterpreterUtils.ToNum<int>(arguments[0]);
            return (double)Random.Shared.Next(max);
        }

        public override string ToString() => GetToString("random", Arity);
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

        public override string ToString() => GetToString("stringify", Arity);
    }

    internal class Num : ILoxCallable
    {
        public int Arity => 1;

        public object Call(Interpreter interpreter, List<object> arguments) => InterpreterUtils.ToNum<double>(arguments[0]);

        public override string ToString() => GetToString("num", Arity);
    }

    internal class Input : ILoxCallable
    {
        public int Arity => 0;

        public object Call(Interpreter interpreter, List<object> arguments) => Console.ReadLine()!;

        public override string ToString() => GetToString("input", Arity);
    }

    internal class ReadFile : ILoxCallable
    {
        public int Arity => 1;

        public object Call(Interpreter interpreter, List<object> arguments) => File.ReadAllText((string)arguments[0]);

        public override string ToString() => GetToString("readFile", Arity);
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

        public override string ToString() => GetToString("len", Arity);
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
        public override string ToString() => GetToString("write", Arity);
    }

    internal class Clear : ILoxCallable
    {
        public int Arity => 0;

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            Console.Clear();
            return null!;
        }

        public override string ToString() =>  GetToString("clear", Arity);
    }

    internal class Int : ILoxCallable
    {
        public int Arity => 1;

        public object Call(Interpreter interpreter, List<object> arguments) 
        {
            double val = InterpreterUtils.ToNum<double>(arguments[0]);
            return Math.Round(val); // The runtime representation is still a double, but this removes the fractional part.
        }

        public override string ToString() => GetToString("int", Arity);
    }

    private static string GetToString(string name, int arity) => $"<native fn {name}{InterpreterUtils.GetVariableString(arity)}>";
}