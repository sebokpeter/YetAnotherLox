using Frontend.Scanner;
using Lox.Interpreter;
using Generated;
using Shared;

namespace Lox;

public class Lox
{
    private static readonly Interpreter.Interpreter _interpreter = new();

    static bool _hadError = false;
    static bool _hadRuntimeError = false;

    protected internal static void Main(string[] args)
    {
        if (args.Length > 1)
        {
            Console.Error.WriteLine("Usage: cslox [script]");
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
            _hadError = false;
        }
    }

    private static void RunFile(string filePath)
    {
        string file = File.ReadAllText(filePath);
        Run(file);
        if (_hadError) System.Environment.Exit(64);
        if (_hadRuntimeError) System.Environment.Exit(70);
    }

    private static void Run(string source)
    {
        Scanner scanner = new(source);
        List<Token> tokens = scanner.ScanTokens();

        Parser.Parser parser = new(tokens);
        List<Stmt> statements = parser.Parse();

        if (_hadError) return;

        Resolver.Resolver resolver = new(_interpreter);
        resolver.Resolve(statements);

        if (_hadError) return;

        _interpreter.Interpret(statements);
    }

    internal static void Error(int line, string msg)
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
        _hadRuntimeError = true;
    }

    private static void Report(int line, string where, string msg)
    {
        Console.Error.WriteLine($"[line {line}] Error{where}: {msg}");
        _hadError = true;
    }
}