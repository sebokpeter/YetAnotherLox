using Generated;
using LoxConsole;
using LoxConsole.Interpreter;
using LoxConsole.Parser;
using LoxConsole.Resolver;
using LoxConsole.Scanner;

internal class Lox
{
    private static readonly Interpreter interpreter = new();

    static bool HadError { get; set; } = false;
    static bool HadRuntimeError {get; set;} = false;

    private static void Main(string[] args)
    {
        if (args.Length > 1)
        {
            Console.Error.WriteLine("Usage: clox [script]");
            System.Environment.Exit(64);
        }
        else if (args.Length == 1)
        {
            RunFile(args[0]);
        }
        else
        {
            RunPrompt();
        }
    }

    private static void RunPrompt()
    {
        while (true)
        {
            Console.Write("> ");
            string? line = Console.ReadLine();
            if (line is null) break;
            Run(line);
            HadError = false;
        }
    }

    private static void RunFile(string filePath)
    {
        string file = File.ReadAllText(filePath);
        Run(file);
        if (HadError) System.Environment.Exit(64);
        if (HadRuntimeError) System.Environment.Exit(70);
    }

    private static void Run(string source)
    {
        Scanner scanner = new(source);
        List<Token> tokens = scanner.ScanTokens();

        Parser parser = new(tokens);
        List<Stmt> statements = parser.Parse();

        if(HadError) return;

        Resolver resolver = new(interpreter);
        resolver.Resolve(statements);

        if(HadError) return;

        interpreter.Interpret(statements);
    }

    public static void Error(int line, string msg)
    {
        Report(line, "", msg);
    }

    internal static void Error(Token token, string msg)
    {
        if (token.Type == TokenType.EOF)
        {
            Report(token.Line, " at end", msg);
        }
        else 
        {
            Report(token.Line, " at '" + token.Lexeme + "'", msg);
        }
    }

    internal static void RuntimeError(RuntimeException ex)
    {
        Console.Error.WriteLine(ex.Message + $" [line {ex.Token.Line}]");
        HadRuntimeError = true; 
    }

    private static void Report(int line, string where, string msg)
    {
        Console.Error.WriteLine($"[line {line}] Error{where}: {msg}");
        HadError = true;
    }


}