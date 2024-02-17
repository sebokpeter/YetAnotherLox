using System.Diagnostics;
using Frontend.Parser;
using Frontend.Scanner;
using Generated;
using LoxVM.Chunk;
using LoxVM.Value;
using Shared;
using Shared.ErrorHandling;
using static LoxVM.Chunk.OpCode;

namespace LoxVM.VM;

internal class Vm : IDisposable
{
    internal List<Error> Errors { get; init; }

    private const int STACK_MAX = 256;

    private Chunk.Chunk? chunk;

    private bool disposed;
    private ushort ip;

    private readonly ValueStack _stack;

    private readonly Dictionary<Obj, LoxValue> _globals;

    public Vm()
    {
        disposed = false;
        _stack = new(STACK_MAX);
        _globals = [];
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
        // TODO: Handle line numbers correctly

        while(true)
        {
#if DEBUG_TRACE_EXECUTION
            _stack.PrintStack();
            chunk!.DisassembleInstruction(ip);
#endif

            OpCode instruction = (OpCode)ReadByte();

            switch(instruction)
            {
                case Return:
                    return InterpretResult.Ok;
                case Constant:
                    LoxValue constant = ReadConstant();
                    _stack.Push(constant);
                    break;
                case Negate:
                    if(!_stack.Peek(0).IsNumber)
                    {
                        AddRuntimeError("Operand must be a number.", chunk!.Lines.Last());
                        return InterpretResult.RuntimeError;
                    }
                    _stack.Push(LoxValue.Number(-_stack.Pop().AsNumber));
                    break;
                case Add or Subtract or Multiply or Divide or Modulo:
                    if(!BinaryOp(instruction))
                    {
                        return InterpretResult.RuntimeError;
                    }
                    break;
                case Nil:
                    _stack.Push(LoxValue.Nil());
                    break;
                case True:
                    _stack.Push(LoxValue.Bool(true));
                    break;
                case False:
                    _stack.Push(LoxValue.Bool(false));
                    break;
                case Not:
                    _stack.Push(LoxValue.Bool(IsFalsey(_stack.Pop())));
                    break;
                case Equal:
                    LoxValue a = _stack.Pop();
                    LoxValue b = _stack.Pop();
                    _stack.Push(LoxValue.Bool(a.Equals(b)));
                    break;
                case Greater or Less:
                    if(!BinaryOp(instruction))
                    {
                        return InterpretResult.RuntimeError;
                    }
                    break;
                case And or Or:
                    if(!BinaryOp(instruction))
                    {
                        return InterpretResult.RuntimeError;
                    }
                    break;
                case Print:
                    Console.WriteLine(_stack.Pop());
                    break;
                case Pop:
                    _stack.Pop();
                    break;
                case DefineGlobal:
                    Obj name = ReadConstant().AsObj;
                    _globals[name] = _stack.Peek(0);
                    _stack.Pop();
                    break;
                case GetGlobal:
                    Obj varName = ReadConstant().AsObj;
                    if(!_globals.TryGetValue(varName, out LoxValue value))
                    {
                        AddRuntimeError($"Undefined variable {varName.AsString}.", chunk!.Lines.Last());
                        return InterpretResult.RuntimeError;
                    }
                    _stack.Push(value);
                    break;
                case SetGlobal:
                    Obj globalName = ReadConstant().AsObj;
                    if(!_globals.ContainsKey(globalName))
                    {
                        AddRuntimeError($"Undefined variable {globalName.AsString}.", chunk!.Lines.Last());
                        return InterpretResult.RuntimeError;
                    }
                    _globals[globalName] = _stack.Peek(0);
                    break;
                case GetLocal:
                    byte getSlot = ReadByte();
                    _stack.Push(_stack[getSlot]);
                    break;
                case SetLocal:
                    byte setSlot = ReadByte();
                    _stack[setSlot] = _stack.Peek(0);
                    break;
                case JumpIfFalse:
                    ushort offset = ReadShort();
                    if(IsFalsey(_stack.Peek(0)))
                    {
                        ip += offset;
                    }
                    break;
                case Jump:
                    ushort jumpOffset = ReadShort();
                    ip += jumpOffset;
                    break;
                default:
                    throw new UnreachableException();
            }
        }
    }

    private static bool IsFalsey(LoxValue loxValue) => loxValue.IsNil || (loxValue.IsBool && !loxValue.AsBool);

    private bool BinaryOp(OpCode op)
    {
        LoxValue a = _stack.Pop();
        LoxValue b = _stack.Pop();

        if(a.IsString || b.IsString)
        {
            // Concat a and b
            // Automatically convert a non-string value (e.g. a number) to string, when one of the operand is string
            string left = a.ToString();
            string right = b.ToString();

            _stack.Push(LoxValue.Object(left + right));
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
            AddRuntimeError("Both operands must be booleans.", chunk!.Lines.Last());
            return false;
        }

        bool left = a.AsBool;
        bool right = b.AsBool;

        bool res = op switch
        {
            And => left && right,
            Or => left || right,
            _ => throw new UnreachableException($"Opcode was {op}.")
        };

        _stack.Push(LoxValue.Bool(res));

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
                Less => left < right,
                Greater => left > right,
                _ => throw new UnreachableException($"Opcode was {op}.")
            };

            _stack.Push(LoxValue.Bool(res));
            return true;
        }
        else
        {
            double res = op switch
            {
                Add => left + right,
                Subtract => left - right,
                Multiply => left * right,
                Divide => left / right,
                Modulo => left % right,
                _ => throw new UnreachableException($"Opcode was {op}.")
            };

            _stack.Push(LoxValue.Number(res));

            return true;
        }
    }

    private LoxValue ReadConstant() => chunk!.Constants[ReadByte()];

    private byte ReadByte() => chunk![ip++];

    private ushort ReadShort()
    {
        ip += 2;
        return (ushort)(chunk![ip-2] << 8 | chunk[ip-1]);
    }

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