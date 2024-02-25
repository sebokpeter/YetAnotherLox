using Shared.ErrorHandling;
using LoxVM.VM;
using Shared;
using Frontend.Scanner;
using Generated;
using Frontend.Parser;
using LoxVM.Value;
using LoxVM.Compiler;

namespace LoxVM;

public class Lox
{
    public static void Main(string[] args)
    {
        Vm vm = new();

        if (args.Length == 0)
        {
            REPL(vm);
        }
        else if (args.Length == 1)
        {
            RunFile(args[0], vm);
        }
        else
        {
            Console.Error.WriteLine("Usage: vmlox [path]");
        }
    }

    private static void RunFile(string path, Vm vm)
    {
        string source;

        try
        {
            source = File.ReadAllText(path);
        }
        catch (FileNotFoundException)
        {
            Console.Error.WriteLine($"File {path} not found.");
            return;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Could not open file {path}: {ex.Message}");
            return;
        }

        InterpretResult result = Interpret(vm, source);

        if (result != InterpretResult.Ok)
        {
            vm.Errors.ReportAll();
            int exitCode = result == InterpretResult.CompileError ? 65 : 70;
            Environment.Exit(exitCode);
        }
    }

    private static void REPL(Vm vm)
    {
        while (true)
        {
            Console.Write("> ");

            string? line = Console.ReadLine();

            if (line is null)
            {
                break;
            }

            InterpretResult result = Interpret(vm, line);

            if (result != InterpretResult.Ok)
            {
                vm.Errors.ReportAll();
            }
        }
    }

    private static InterpretResult Interpret(Vm vm, string source)
    {
        (bool scanSuccess, List<Token>? tokens) = Scan(source);

        if (!scanSuccess)
        {
            return InterpretResult.CompileError;
        }

        (bool parseSuccess, List<Stmt>? statements) = Parse(tokens!);

        if (!parseSuccess)
        {
            return InterpretResult.CompileError;
        }

        (bool compileSuccess, ObjFunction? function) = Compile(statements!);

        if (!compileSuccess)
        {
            return InterpretResult.CompileError;
        }

        return vm.Interpret(function!);
    }


    private static (bool success, List<Token>? tokens) Scan(string source)
    {
        Scanner scanner = new(source);
        List<Token> tokens = scanner.ScanTokens();

        if (scanner.HadError)
        {
            scanner.Errors.ReportAll();
            return (false, null);
        }

        return (true, tokens);
    }

    private static (bool success, List<Stmt>? statements) Parse(List<Token> tokens)
    {
        Parser parser = new(tokens);
        List<Stmt> statements = parser.Parse();

        if (parser.HadError)
        {
            parser.Errors.ReportAll();
            return (false, null);
        }

        return (true, statements);
    }

    private static (bool success, ObjFunction? topLevelFunction) Compile(List<Stmt> statements)
    {
        BytecodeCompiler compiler = new(statements);
        ObjFunction function = compiler.Compile();

        if (compiler.HadError)
        {
            compiler.Errors.ReportAll();
            return (false, null);
        }

#if DEBUG_PRINT_CODE
        function!.Chunk.Disassemble("script");
#endif

        return (true, function);
    }
}