using Frontend.Scanner;
using Lox.Interpreter;
using Shared;
using Generated;
using Frontend.Parser;
using Shared.ErrorHandling;

namespace Lox;

public class Lox
{
    private static readonly Interpreter.Interpreter _interpreter = new();

    static bool _hadError = false;
    static bool _hadRuntimeError = false;

    protected internal static void Main(string[] args)
    {
        if(args.Length > 1)
        {
            Console.Error.WriteLine("Usage: cslox [script]");
            System.Environment.Exit(64);
        }
        else if(args.Length == 1)
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
        while(true)
        {
            Console.Write("> ");
            string? line = Console.ReadLine();
            if(line is null) break;
            Run(line);
            _hadError = false;
        }
    }

    private static void RunFile(string filePath)
    {
        string file = File.ReadAllText(filePath);
        Run(file);
        if(_hadError) System.Environment.Exit(64);
        if(_hadRuntimeError) System.Environment.Exit(70);
    }

    private static void Run(string source)
    {
        List<Stmt> statements = ScanAndParse(source);

        if(_hadError) return;

        Resolve(statements);

        if(_hadError) return;

        _interpreter.Interpret(statements);

        if(_interpreter.HadError)
        {
            _interpreter.Error.Report();
            _hadRuntimeError = true;
        }
    }

    private static void Resolve(List<Stmt> stmts)
    {
        Resolver.Resolver resolver = new(_interpreter);
        resolver.Resolve(stmts);

        if(resolver.HadError)
        {
            resolver.Errors.ReportAll();
            _hadError = true;
        }
    }

    private static List<Stmt> ScanAndParse(string source)
    {
        // Keep scanning and parsing passes as separate, instead of combining them in the Frontend, so we can report an error after each step, and stop the compilation/execution.
        Scanner scanner = new(source);
        List<Token> tokens = scanner.ScanTokens();

        if(scanner.HadError)
        {
            scanner.Errors.ReportAll();
            _hadError = true;
        }

        Parser parser = new(tokens);
        List<Stmt> statements = parser.Parse();

        if(parser.HadError)
        {
            parser.Errors.ReportAll();
            _hadError = true;
        }

        return statements;
    }
}