using System.Diagnostics;
using Frontend.Parser;
using Frontend.Scanner;
using Generated;
using LoxVM.Chunk;
using LoxVM.Compiler;
using LoxVM.Value;
using Shared;
using Shared.ErrorHandling;
using static LoxVM.Chunk.OpCode;

namespace LoxVM.VM;

internal class Vm : IDisposable
{
    internal List<Error> Errors { get; init; }

    private const int FRAMES_MAX = 64;
    private const int STACK_MAX = FRAMES_MAX * byte.MaxValue;

    private bool disposed;

    private CallFrame[] callFrames;
    private int frameCount;

    private ref CallFrame Frame => ref callFrames[frameCount - 1];

    private readonly VmStack<LoxValue> _stack;
    private readonly Dictionary<Obj, LoxValue> _globals;

    private readonly Stopwatch _stopwatch;

    private ObjUpValue? openUpValues;

    public Vm()
    {
        disposed = false;
        callFrames = new CallFrame[FRAMES_MAX];
        _stack = new(STACK_MAX);
        _globals = [];
        Errors = [];
        _stopwatch = new();

        DefineNative("clock", (_) => LoxValue.Number(_stopwatch.ElapsedMilliseconds), 0);
        DefineNative("add", (argNum) =>
        {
            LoxValue a = _stack[argNum];
            LoxValue b = _stack[argNum + 1];
            _stack.PrintStack();
            return LoxValue.Number(a.AsNumber - b.AsNumber);
        }, 2);
    }

    internal InterpretResult Interpret(string source)
    {
        (bool success, ObjFunction? function) = Compile(source);
        if(!success)
        {
            return InterpretResult.CompileError;
        }

        _stopwatch.Restart();

        ObjClosure closure = new() {Function = function!, UpValues = []};
        _stack.Push(LoxValue.Object(closure));
        CallFn(closure, 0);

        return Run();
    }

    private (bool success, ObjFunction? function) Compile(string source)
    {
        ResetVm();

        (bool scanSuccess, List<Token>? tokens) = Scan(source);

        if(!scanSuccess)
        {
            return (false, null);
        }

        (bool parseSuccess, List<Stmt>? statements) = Parse(tokens!);

        if(!parseSuccess)
        {
            return (false, null);
        }

        // TODO: Emit bytecode based on AST
        (bool compileSuccess, ObjFunction? function) = CompileStatements(statements!);

        if(!compileSuccess)
        {
            return (false, null);
        }


#if DEBUG_PRINT_CODE
        function!.Chunk.Disassemble(String.IsNullOrEmpty(function.Name) ? "script" : function.Name);
        Console.WriteLine("================");
#endif

        return (true, function);
    }

    private (bool compileSuccess, ObjFunction? chunk) CompileStatements(List<Stmt> stmts)
    {
        BytecodeCompiler compiler = new(stmts);
        ObjFunction function = compiler.Compile();

        if(compiler.HadError)
        {
            Errors.AddRange(compiler.Errors);
            return (false, null);
        }

        return (true, function);
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
            Frame.Closure.Function.Chunk.DisassembleInstruction(Frame.Ip);
            Console.WriteLine("-------------------------");
#endif

            OpCode instruction = (OpCode)Frame.ReadByte();

            switch(instruction)
            {
                case Return:
                    int newTop = Frame.Slot;
                    LoxValue result = _stack.Pop();
                    CloseUpValues(newTop);
                    frameCount--;
                    if(frameCount == 0)
                    {
                        _stack.Pop();
                        return InterpretResult.Ok;
                    }
                    _stack.StackTop = newTop;
                    _stack.Push(result);
                    break;
                case Constant:
                    LoxValue constant = Frame.ReadConstant();
                    _stack.Push(constant);
                    break;
                case Negate:
                    if(!_stack.Peek(0).IsNumber)
                    {
                        AddRuntimeError("Operand must be a number.");
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
                    Obj name = Frame.ReadConstant().AsObj;
                    _globals[name] = _stack.Pop();
                    break;
                case GetGlobal:
                    Obj varName = Frame.ReadConstant().AsObj;
                    if(!_globals.TryGetValue(varName, out LoxValue? value))
                    {
                        AddRuntimeError($"Undefined variable {varName.AsString}.");
                        return InterpretResult.RuntimeError;
                    }
                    _stack.Push(value);
                    break;
                case SetGlobal:
                    Obj globalName = Frame.ReadConstant().AsObj;
                    if(!_globals.ContainsKey(globalName))
                    {
                        AddRuntimeError($"Undefined variable {globalName.AsString}.");
                        return InterpretResult.RuntimeError;
                    }
                    _globals[globalName] = _stack.Peek(0);
                    break;
                case GetLocal:
                    byte getSlot = Frame.ReadByte();
                    _stack.Push(_stack[Frame.Slot + getSlot]);
                    break;
                case SetLocal:
                    byte setSlot = Frame.ReadByte();
                    _stack[Frame.Slot + setSlot] = _stack.Peek(0);
                    break;
                case JumpIfFalse:
                    ushort offset = Frame.ReadShort();
                    if(IsFalsey(_stack.Peek(0)))
                    {
                        Frame.Ip += offset;
                    }
                    break;
                case Jump:
                    ushort jumpOffset = Frame.ReadShort();
                    Frame.Ip += jumpOffset;
                    break;
                case Loop:
                    ushort loopOffset = Frame.ReadShort();
                    Frame.Ip -= loopOffset;
                    break;
                case Call:
                    int argCount = Frame.ReadByte();
                    if(!CallValue(_stack.Peek(argCount), argCount))
                    {
                        return InterpretResult.RuntimeError;
                    }
                    break;
                case Closure:
                    CreateClosure();
                    break;
                case SetUpValue:
                    byte setUpValueSlot = Frame.ReadByte();
                    Frame.Closure.UpValues[setUpValueSlot].LoxValue = _stack.Peek(0);
                    break;
                case GetUpValue:
                    byte getUpvalueSlot = Frame.ReadByte();
                    _stack.Push(Frame.Closure.UpValues[getUpvalueSlot].LoxValue);
                    break;
                case CloseUpValue:
                    CloseUpValues(_stack.StackTop-1);
                    _stack.Pop();
                    break;
                case Class:
                    _stack.Push(LoxValue.Object(new ObjClass() {Name = Frame.ReadString()}));
                    break;
                default:
                    throw new UnreachableException();
            }
        }
    }

    private void CloseUpValues(int locationOnStack)
    {
        LoxValue value = _stack[locationOnStack];

        while(openUpValues is not null && locationOnStack > _stack.StackTop)
        {
            ObjUpValue upValue = openUpValues;
            upValue.Closed = value;
            upValue.LoxValue = value;
            openUpValues = upValue.Next;
        }
    }

    private void CreateClosure()
    {
        ObjFunction function = Frame.ReadConstant().AsObj.AsFunction;

        List<ObjUpValue> upValues = Enumerable.Range(0, function.UpValueCount).Select<int, ObjUpValue>(_ => null!).ToList();
        ObjClosure closure = new() {Function = function, UpValues = upValues};  
        _stack.Push(LoxValue.Object(closure));

        for(int i = 0; i < function.UpValueCount; i++)
        {
            bool isLocal = Frame.ReadByte() == 1;
            byte index = Frame.ReadByte();

            if(isLocal)
            {
                closure.UpValues[i] = CaptureValue(_stack[Frame.Slot + index]);
            }
            else
            {
                closure.UpValues[i] = Frame.Closure.UpValues[index];
            }
        }
    }

    private ObjUpValue CaptureValue(LoxValue value)
    {
        ObjUpValue? prevUpValue = null;
        ObjUpValue? upValue = openUpValues;
        while(upValue is not null)
        {
            prevUpValue = upValue;
            upValue = upValue.Next;
        }

        if(upValue is not null && upValue.LoxValue == value)
        {
            return upValue;
        }

        ObjUpValue createdUpValue = new() { LoxValue = value, Next = upValue };
        if(prevUpValue is null)
        {
            openUpValues = createdUpValue;
        } 
        else 
        {
            prevUpValue.Next = createdUpValue;
        }

        return createdUpValue;
    }

    private bool CallValue(LoxValue c, int argCount)
    {
        if(c.IsObj)
        {
            Obj callee = c.AsObj;
            switch(callee.Type)
            {
                case ObjType.Class:
                    ObjClass objClass = callee.AsClass;
                    _stack[_stack.StackTop - argCount - 1] = LoxValue.Object(new ObjInstance() {ObjClass = objClass, Fields = []});
                    return true;
                case ObjType.Closure:
                    return CallFn(callee.AsClosure, argCount);
                case ObjType.Native:
                    return CallNative(callee.AsNative, argCount);
                default:
                    break;
            }
        }
        AddRuntimeError("Can only call functions and classes.");
        return false;
    }

    private bool CallNative(ObjNativeFn nativeFn, int argCount)
    {
        if(argCount != nativeFn.Arity)
        {
            AddRuntimeError($"Expected {nativeFn.Arity} arguments but got {argCount}.");
            return false;
        }

        LoxValue result = nativeFn.Func.Invoke(_stack.StackTop - argCount);
        _stack.StackTop -= argCount + 1;
        _stack.Push(result);
        return true;
    }

    private bool CallFn(ObjClosure closure, int argCount)
    {
        if(argCount != closure.Function.Arity)
        {
            AddRuntimeError($"Expected {closure.Function.Arity} arguments but got {argCount}.");
            return false;
        }

        if(frameCount == FRAMES_MAX)
        {
            AddRuntimeError("Stack overflow.");
            return false;
        }

        CallFrame callFrame = new() { Closure = closure, Ip = 0, Slot = (ushort)(_stack.StackTop - argCount - 1) };
        callFrames[frameCount++] = callFrame;
        return true;
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
            AddRuntimeError("Both operands must be booleans.");
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
            AddRuntimeError("Both operands must be numbers.");
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
        Errors.Clear();
        callFrames = new CallFrame[STACK_MAX];
        _stack.Reset();
        openUpValues = null;
    }

    private void AddRuntimeError(string message)
    {
        IEnumerable<Frame> frames = callFrames.Take(frameCount - 1).Reverse().Select(frame =>
        {
            ObjFunction function = frame.Closure.Function;

            return new Frame(function.Chunk.Lines[frame.Ip], String.IsNullOrEmpty(function.Name) ? "script" : function.Name + "()");
        });

        CallFrame callFrame = callFrames[frameCount - 1];
        Errors.Add(new RuntimeError(message, callFrame.Closure.Function.Chunk.Lines[callFrame.Ip], null, new Shared.ErrorHandling.StackTrace(frames)));
    }

    private void DefineNative(string name, Func<int, LoxValue> native, int arity)
    {
        Obj nameObj = LoxValue.Object(name).AsObj;
        ObjNativeFn objNativeFn = new() { Arity = arity, Name = name, Func = native };
        LoxValue natFn = LoxValue.Object(objNativeFn);
        _globals.Add(nameObj, natFn);
    }
}

// TODO: Test performance with class and readonly struct
// This is a mutable value type - try to make it immutable?
internal struct CallFrame
{
    //internal ObjFunction Function { get; init; }
    internal ObjClosure Closure { get; init; }
    internal ushort Ip { get; set; }
    internal ushort Slot { get; init; }

    internal byte ReadByte() => Closure.Function.Chunk[Ip++];
    internal LoxValue ReadConstant() => Closure.Function.Chunk.Constants[ReadByte()];
    internal ushort ReadShort()
    {
        Ip += 2;
        return (ushort)(Closure.Function.Chunk[Ip - 2] << 8 | Closure.Function.Chunk[Ip - 1]);
    }
    internal string ReadString() => ReadConstant().AsObj.AsString.StringValue;
}

internal enum InterpretResult
{
    Ok,
    CompileError,
    RuntimeError
}