using LoxVM.VM;

namespace LoxVM;

public class Lox
{
    public static void Main(string[] args)
    {
        using Vm vm = new();

        if(args.Length == 0)
        {
            REPL(vm);
        }
        else if(args.Length == 1)
        {
            RunFile(args[0], vm);
        }
        else
        {
            Console.Error.WriteLine("Usage: cslox [path]");
        }
    }

    private static void RunFile(string path, Vm vm)
    {
        string source;

        try
        {
            source = File.ReadAllText(path);
        }
        catch(FileNotFoundException)
        {
            Console.Error.WriteLine($"File {path} not found.");
            return;
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine($"Could not open file {path}: {ex.Message}");
            return;
        }

        InterpretResult result = vm.Interpret(source);

        if(result == InterpretResult.CompileError)
        {
            ReportErrors(vm.Errors);
            Environment.Exit(65);
        }
        else if(result == InterpretResult.RuntimeError)
        {
            Environment.Exit(70);
        }
    }

    private static void REPL(Vm vm)
    {
        while(true)
        {
            Console.Write("> ");

            string? line = Console.ReadLine();

            if(line is null)
            {
                break;
            }

            InterpretResult result = vm.Interpret(line);

            if(result == InterpretResult.CompileError)
            {
                ReportErrors(vm.Errors);
            }
        }
    }

    private static void ReportErrors(IEnumerable<string> errors)
    {
        foreach(string err in errors)
        {
            Console.Error.WriteLine(err);
        }
    }

}