using System.Diagnostics;
using Frontend.Parser;
using Frontend.Scanner;
using Generated;
using LoxVM.Chunk;
using LoxVM.Value;
using Shared;
using Shared.ErrorHandling;

namespace LoxVM.VM;

internal class Vm : IDisposable
{
    private const int STACK_MAX = 256;

    private Chunk.Chunk? chunk;

    private bool disposed;
    private byte ip;

    private readonly Stack<LoxValue> _stack;

    internal List<Error> Errors { get; init; }

    public Vm()
    {
        disposed = false;
        _stack = new(STACK_MAX);
        Errors = [];
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
        ResetVm();

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
        this.chunk = chunk;
        return true;
    }

    private (bool compileSuccess, Chunk.Chunk? chunk) CompileStatements(List<Stmt> stmts)
    {
        BytecodeEmitter emitter = new(stmts);
        Chunk.Chunk chunk = emitter.EmitBytecode();

        if(emitter.HadError)
        {
            Errors.AddRange(emitter.Errors);
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
            Errors.AddRange(scanner.Errors);
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
            Errors.AddRange(parser.Errors);
            return (false, null);
        }

        return (true, statements);
    }

    private InterpretResult Run()
    {
        while(true)
        {
#if DEBUG_TRACE_EXECUTION
            _stack.PrintStack();
            chunk!.DisassembleInstruction(ip);
#endif

            OpCode instruction = (OpCode)ReadByte();

            switch(instruction)
            {
                case OpCode.Return:
                    Console.WriteLine(Pop());
                    return InterpretResult.Ok;
                case OpCode.Constant:
                    LoxValue constant = ReadConstant();
                    Push(constant);
                    break;
                case OpCode.Negate:
                    if(!Peek(0).IsNumber)
                    {
                        AddRuntimeError("Operand must be a number.", chunk!.Lines.Last());
                        return InterpretResult.RuntimeError;
                    }
                    Push(LoxValue.CreateNumberValue(-Pop().AsNumber));
                    break;
                case OpCode.Add or OpCode.Subtract or OpCode.Multiply or OpCode.Divide or OpCode.Modulo:
                    if(!BinaryOp(instruction))
                    {
                        return InterpretResult.RuntimeError;
                    }
                    break;
                case OpCode.Nil:
                    Push(LoxValue.CreateNilValue());
                    break;
                case OpCode.True:
                    Push(LoxValue.CreateBoolValue(true));
                    break;
                case OpCode.False:
                    Push(LoxValue.CreateBoolValue(false));
                    break;
                case OpCode.Not:
                    Push(LoxValue.CreateBoolValue(IsFalsey(Pop())));
                    break;
                case OpCode.Equal:
                    LoxValue a = Pop();
                    LoxValue b = Pop();
                    Push(LoxValue.CreateBoolValue(a.Equals(b)));
                    break;
                case OpCode.Greater or OpCode.Less:
                    if(!BinaryComparison(instruction))
                    {
                        return InterpretResult.RuntimeError;
                    }
                    break;
                default:
                    throw new UnreachableException();
            }
        }
    }

    private static bool IsFalsey(LoxValue loxValue) => loxValue.IsNil || (loxValue.IsBool && !loxValue.AsBool);

    private bool BinaryComparison(OpCode op)
    {
        LoxValue a = Pop();
        LoxValue b = Pop();

        if(!(a.IsNumber && b.IsNumber))
        {
            AddRuntimeError("Both operands must be numbers", chunk!.Lines.Last());
            return false;
        }

        double left = a.AsNumber;
        double right = b.AsNumber;

        bool res = op switch
        {
            OpCode.Greater  => left > right,
            OpCode.Less     => left < right,
            _ => throw new ArgumentException($"{op} is not a valid binary operator opcode.", nameof(op))
        };

        Push(LoxValue.CreateBoolValue(res));

        return true;
    }
    
    private bool BinaryOp(OpCode op)
    {
        LoxValue a = Pop();
        LoxValue b = Pop();

        if(!(a.IsNumber && b.IsNumber))
        {
            AddRuntimeError("Both operands must be numbers", chunk!.Lines.Last());
            return false;
        }

        double left = a.AsNumber;
        double right = b.AsNumber;

        double res = op switch
        {
            OpCode.Add => left + right,
            OpCode.Subtract => left - right,
            OpCode.Multiply => left * right,
            OpCode.Divide => left / right,
            OpCode.Modulo => left % right,
            _ => throw new ArgumentException($"{op} is not a valid binary operator opcode.", nameof(op))
        };

        Push(LoxValue.CreateNumberValue(res));
        
        return true;
    }

    private void Push(LoxValue value) => _stack.Push(value);

    private LoxValue Pop() => _stack.Pop();

    private LoxValue Peek(int distance) => _stack.ElementAt(distance);

    private LoxValue ReadConstant() => chunk!.Constants[ReadByte()];

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
        Errors.Clear();
        ip = 0;
    }

    private void AddRuntimeError(string message, int line) => Errors.Add(new RuntimeError(message, line, null));
}

internal enum InterpretResult
{
    Ok,
    CompileError,
    RuntimeError
}