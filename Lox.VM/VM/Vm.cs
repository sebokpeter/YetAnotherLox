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
                    Push(LoxValue.Number(-Pop().AsNumber));
                    break;
                case OpCode.Add or OpCode.Subtract or OpCode.Multiply or OpCode.Divide or OpCode.Modulo:
                    if(!BinaryOp(instruction))
                    {
                        return InterpretResult.RuntimeError;
                    }
                    break;
                case OpCode.Nil:
                    Push(LoxValue.Nil());
                    break;
                case OpCode.True:
                    Push(LoxValue.Bool(true));
                    break;
                case OpCode.False:
                    Push(LoxValue.Bool(false));
                    break;
                case OpCode.Not:
                    Push(LoxValue.Bool(IsFalsey(Pop())));
                    break;
                case OpCode.Equal:
                    LoxValue a = Pop();
                    LoxValue b = Pop();
                    Push(LoxValue.Bool(a.Equals(b)));
                    break;
                case OpCode.Greater or OpCode.Less:
                    if(!BinaryOp(instruction))
                    {
                        return InterpretResult.RuntimeError;
                    }
                    break;
                case OpCode.And or OpCode.Or:
                    if(!BinaryOp(instruction))
                    {
                        return InterpretResult.RuntimeError;
                    }
                    break;
                case OpCode.Print:
                    Console.WriteLine(Pop());
                    break;
                case OpCode.Pop:
                    Pop();
                    break;
                default:
                    throw new UnreachableException();
            }
        }
    }

    private static bool IsFalsey(LoxValue loxValue) => loxValue.IsNil || (loxValue.IsBool && !loxValue.AsBool);

    private bool BinaryOp(OpCode op)
    {
        LoxValue a = Pop();
        LoxValue b = Pop();

        if(a.IsString || b.IsString)
        {
            // Concat a and b
            // Automatically convert a non-string value (e.g. a number) to string, when one of the operand is string
            string left = a.ToString();
            string right = b.ToString();

            Push(LoxValue.Object(left + right));
            return true;
        }
        else if(a.IsNumber)
        {
            return HandleNum(a, b, op);
        }
        else if(a.IsBool)
        {
            return HandleBool(a, b, op);
        }
        else
        {
            throw new UnreachableException($"Operand {a} is neither a string, number, or bool.");
        }
    }

    private bool HandleBool(LoxValue a, LoxValue b, OpCode op)
    {
        if(!b.IsBool)
        {
            AddRuntimeError("Both operands must be numbers.", chunk!.Lines.Last());
            return false;
        }

        bool left = a.AsBool;
        bool right = b.AsBool;

        bool res = op switch
        {
            OpCode.And => left && right,
            OpCode.Or => left || right,
            _ => throw new UnreachableException($"Opcode was {op}.")
        };

        Push(LoxValue.Bool(res));

        return true;
    }

    private bool HandleNum(LoxValue a, LoxValue b, OpCode op)
    {
        if(!b.IsNumber)
        {
            AddRuntimeError("Both operands must be numbers.", chunk!.Lines.Last());
            return false;
        }

        double left = a.AsNumber;
        double right = b.AsNumber;

        if(op.IsComparisonOp())
        {
            bool res = op switch
            {
                OpCode.Less => left < right,
                OpCode.Greater => left > right,
                _ => throw new UnreachableException($"Opcode was {op}.")
            };

            Push(LoxValue.Bool(res));
            return true;
        }
        else
        {
            double res = op switch
            {
                OpCode.Add => left + right,
                OpCode.Subtract => left - right,
                OpCode.Multiply => left * right,
                OpCode.Divide => left / right,
                OpCode.Modulo => left % right,
                _ => throw new UnreachableException($"Opcode was {op}.")
            };

            Push(LoxValue.Number(res));

            return true;
        }

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