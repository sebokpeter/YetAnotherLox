using System.Diagnostics;
using Frontend.Parser;
using Frontend.Scanner;
using Generated;
using LoxVM.Chunk;
using Shared;

namespace LoxVM.VM;

internal class Vm : IDisposable
{
    internal IEnumerable<string> Errors => _errors.AsEnumerable();

    private const int STACK_MAX = 256;

    private Chunk.Chunk? chunk;

    private bool disposed;
    private byte ip;

    private readonly Value.Value[] _stack;
    private byte stackTop;

    private readonly List<string> _errors; // TODO: Use custom Error object for better reporting

    public Vm()
    {
        disposed = false;
        _stack = new Value.Value[STACK_MAX];
        _errors = [];
        stackTop = 0;
    }

    internal InterpretResult Interpret(string source)
    {
        if(!Compile(source))
        {
            return InterpretResult.CompileError;
        }

        InterpretResult result = Run();

        return result;
    }

    private bool Compile(string source)
    {
        (bool scanSuccess, List<Token>? tokens) = Scan(source);

        if(!scanSuccess)
        {
            return false;
        }

        (bool parseSuccess, List<Stmt>? statements) = Parse(tokens!);

        if(!parseSuccess)
        {
            return false;
        }

        // TODO: Emit bytecode based on AST
        (bool compileSuccess, Chunk.Chunk? chunk) = CompileStatements(statements!);

        if(!compileSuccess)
        {
            return false;
        }

#if DEBUG_PRINT_CODE
        chunk!.Disassemble("Code");
#endif

        ResetVm();

        this.chunk = chunk;
        return true;
    }

    private (bool compileSuccess, Chunk.Chunk? chunk) CompileStatements(List<Stmt> stmts)
    {
        BytecodeEmitter emitter = new(stmts);
        Chunk.Chunk chunk = emitter.EmitBytecode();

        if(emitter.HadError)
        {
            foreach(CompilationError err in emitter.Errors)
            {
                _errors.Add($"Compilation failed at '{err.Token.Lexeme}': {err.Message} [{err.Token.Line}]");
            }
            return (false, null);
        }

        return (true, chunk);
    }

    private (bool, List<Token>?) Scan(string source)
    {
        Scanner scanner = new(source);
        List<Token> tokens = scanner.ScanTokens();

        if(scanner.HadError)
        {
            foreach(ScannerError err in scanner.Errors)
            {
                _errors.Add($"Scanning failed: {err.Message} [{err.Line}]");
            }
            return (false, null);
        }

        return (true, tokens);
    }

    private (bool, List<Stmt>?) Parse(List<Token> tokens)
    {
        Parser parser = new(tokens!);
        List<Stmt> statements = parser.Parse();

        if(parser.HadError)
        {
            foreach(ParseError err in parser.Errors)
            {
                _errors.Add($"Parsing failed at '{err.Token.Lexeme}': {err.Message}  [{err.Token.Line}]");
            }
            return (false, null);
        }

        return (true, statements);
    }

    private InterpretResult Run()
    {
        while(true)
        {
#if DEBUG_TRACE_EXECUTION
            _stack.PrintStack(stackTop);
            chunk!.DisassembleInstruction(ip);
#endif

            OpCode instruction = (OpCode)ReadByte();

            switch(instruction)
            {
                case OpCode.Return:
                    Console.WriteLine(Pop());
                    return InterpretResult.Ok;
                case OpCode.Constant:
                    Value.Value constant = ReadConstant();
                    Push(constant);
                    break;
                case OpCode.Negate:
                    Value.Value val = Pop();
                    val = new(-val.Val);
                    Push(val);
                    break;
                case OpCode.Add: BinaryOp(OpCode.Add); break;
                case OpCode.Subtract: BinaryOp(OpCode.Subtract); break;
                case OpCode.Multiply: BinaryOp(OpCode.Multiply); break;
                case OpCode.Divide: BinaryOp(OpCode.Divide); break;
                case OpCode.Modulo: BinaryOp(OpCode.Modulo); break;
                default:
                    throw new UnreachableException();
            }
        }
    }

    private void BinaryOp(OpCode op)
    {
        double a = Pop().Val;
        double b = Pop().Val;

        double res = op switch
        {
            OpCode.Add => a + b,
            OpCode.Subtract => a - b,
            OpCode.Multiply => a * b,
            OpCode.Divide => a / b,
            OpCode.Modulo => a % b,
            _ => throw new ArgumentException($"{op} is not a valid binary operator opcode.", nameof(op))
        };

        Push(new(res));
    }

    private void Push(Value.Value value) => _stack[stackTop++] = value;

    private Value.Value Pop() => _stack[--stackTop];

    private Value.Value ReadConstant() => chunk!.Constants[ReadByte()];

    private byte ReadByte() => chunk![ip++];

    public void Dispose() // Implement IDisposable instead of freeVM(), even though there are no unmanaged resources
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if(disposed)
        {
            return;
        }

        if(disposing)
        {
            ResetVm();
        }

        disposed = true;
    }

    private void ResetVm()
    {
        chunk?.FreeChunk();
        ip = 0;
        stackTop = 0;
    }
}

internal enum InterpretResult
{
    Ok,
    CompileError,
    RuntimeError
}