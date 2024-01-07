using System.Numerics;
using System.Text;
using LoxConsole.Interpreter;

internal class NativeFunction
{
    internal class Sleep() : ILoxCallable
    {
        public int Arity => 1;

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            int ms = ToNum<int>(arguments[0]);
            Thread.Sleep(ms);

            return null!;
        }

        public override string ToString() => $"<native fn sleep{GetVariableString(Arity)}>";
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
            int max = ToNum<int>(arguments[0]);
            return (double)Random.Shared.Next(max);
        }

        public override string ToString() => $"<native fn random{GetVariableString(Arity)}>";
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

        public override string ToString() => $"<native fn stringify{GetVariableString(Arity)}>";
    }

    internal class Num : ILoxCallable
    {
        public int Arity => 1;

        public object Call(Interpreter interpreter, List<object> arguments) => ToNum<double>(arguments[0]);

        public override string ToString() => $"<native fn num{GetVariableString(Arity)}>";
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

        public override string ToString() => $"<native fn readFile{GetVariableString(Arity)}>";
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

        public override string ToString() => $"<native fn len{GetVariableString(Arity)}>";
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
        public override string ToString() => $"<native fn write{GetVariableString(Arity)}>";
    }

    internal class Int : ILoxCallable
    {
        public int Arity => 1;

        public object Call(Interpreter interpreter, List<object> arguments) 
        {
            double val = ToNum<double>(arguments[0]);
            return Math.Round(val); // The runtime representation is still a double, but this removes the fractional part.
        }

        public override string ToString() => $"<native fn int{GetVariableString(Arity)}>";
    }

    private static T ToNum<T>(object p) where T: INumber<T> 
    {
        static T TryParseString(string s)
        {
            if (!T.TryParse(s, null, out T? res))
            {
                throw new Exception($"Can't convert '{s}' to number!");
            }
            return res;
        }

        if (p is T num)
        {
            return num;
        }

        if(p is string s)
        {
            return TryParseString(s);
        }

        string? str = p.ToString();

        if(str is not null)
        {
            return TryParseString(str);
        }

        throw new Exception($"Can't convert '{p}' to number!");
    }

    private static string GetVariableString(int arity)
    {
        StringBuilder varBuilder = new();
        varBuilder.Append('(');
        varBuilder.Append(String.Join(", ", Enumerable.Repeat("var", arity)));
        varBuilder.Append(')');
        return varBuilder.ToString();
    }
}