using System.Diagnostics;
using System.Net.Sockets;

using LoxVM.Chunk;
using LoxVM.Value;

using Shared.ErrorHandling;

using static LoxVM.Chunk.OpCode;

namespace LoxVM.VM;

internal class Vm
{
    internal List<Error> Errors { get; init; }

    private const string INIT_STRING = "init";

    private const int FRAMES_MAX = 64;
    private const int STACK_MAX = FRAMES_MAX * byte.MaxValue;

    private CallFrame[] callFrames;
    private int frameCount;

    private ref CallFrame Frame => ref callFrames[frameCount - 1];

    private readonly VmStack<LoxValue> _stack;
    private readonly Dictionary<Obj, LoxValue> _globals;

    private readonly Stopwatch _stopwatch;

    private ObjUpValue? openUpValues;

    public Vm()
    {
        Errors = [];

        callFrames = new CallFrame[FRAMES_MAX];

        _stack = new(STACK_MAX);
        _globals = [];
        _stopwatch = new();

        DefineNativeMethods();
    }

    private void DefineNativeMethods()
    {
        DefineNative("clock", 0, (_) => (LoxValue.Number(_stopwatch.ElapsedMilliseconds), true));
        DefineNative("len", 1, (argLocation) =>
        {
            LoxValue val = _stack[argLocation];

            if (!val.IsObj || !(val.AsObj.IsType(ObjType.String) || val.AsObj.IsType(ObjType.Array)))
            {
                AddRuntimeError("Can only get the length of strings and arrays.");
                return (null, false);
            }

            Obj obj = val.AsObj;

            if (obj.IsType(ObjType.String))
            {
                return (LoxValue.Number(obj.AsString.StringValue.Length), true);
            }
            else
            {
                return (LoxValue.Number(obj.AsArray.Array.Count), true);
            }
        });
        DefineNative("int", 1, (argLocation) =>
        {
            LoxValue value = _stack[argLocation];

            if (!value.IsNumber)
            {
                AddRuntimeError("'int()' can only called on numbers.");
                return (null, false);
            }

            return (LoxValue.Number(Convert.ToInt32(value.AsNumber)), true);
        });
        DefineNative("write", 1, (argLocation) =>
        {
            LoxValue value = _stack[argLocation];
            Console.Write(value);
            return (LoxValue.Nil(), true);
        });
        DefineNative("random", 1, (argLocation) =>
        {
            LoxValue maxValue = _stack[argLocation];

            if (!maxValue.IsNumber || !(maxValue.AsNumber % 1 == 0))
            {
                AddRuntimeError("'max' argument must be an integer.");
                return (null, false);
            }

            return (LoxValue.Number(Random.Shared.Next((int)maxValue.AsNumber)), true);
        });
        DefineNative("clear", 0, (_) =>
        {
            Console.Clear();
            return (LoxValue.Nil(), true);
        });
        DefineNative("sleep", 1, (argLocation) =>
        {
            LoxValue sleepValue = _stack[argLocation];

            if (!sleepValue.IsNumber || !(sleepValue.AsNumber % 1 == 0))
            {
                AddRuntimeError("Argument must be an integer.");
                return (null, false);
            }

            Thread.Sleep((int)sleepValue.AsNumber);
            return (LoxValue.Nil(), true);
        });
    }

    internal InterpretResult Interpret(ObjFunction function)
    {
        ResetVm();

        _stopwatch.Restart();

        ObjClosure closure = new() { Function = function, UpValues = [] };
        _stack.Push(LoxValue.Object(closure));
        CallFn(closure, 0);

        return Run();
    }

    private InterpretResult Run()
    {
        while (true)
        {
#if DEBUG_TRACE_EXECUTION
            _stack.PrintStack();
            Frame.Closure.Function.Chunk.DisassembleInstruction(Frame.Ip);
            Console.WriteLine("-------------------------");
#endif

            OpCode instruction = (OpCode)Frame.ReadByte();

            switch (instruction)
            {
                case Return:
                    int newTop = Frame.Slot;
                    LoxValue result = _stack.Pop();
                    CloseUpValues(newTop);
                    frameCount--;
                    if (frameCount == 0)
                    {
                        _stack.Pop();
                        return InterpretResult.Ok;
                    }
                    _stack.StackTop = newTop;
                    _stack.Push(result);
                    break;
                case Constant:
                    LoxValue constant = LoxValue.FromLoxValue(Frame.ReadConstant());
                    _stack.Push(constant);
                    break;
                case Negate:
                    if (!_stack.Peek(0).IsNumber)
                    {
                        AddRuntimeError("Operand must be a number.");
                        return InterpretResult.RuntimeError;
                    }
                    _stack.Push(LoxValue.Number(-_stack.Pop().AsNumber));
                    break;
                case Add or Subtract or Multiply or Divide or Modulo:
                    if (!BinaryOp(instruction))
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
                    if (!BinaryOp(instruction))
                    {
                        return InterpretResult.RuntimeError;
                    }
                    break;
                case And or Or:
                    if (!BinaryOp(instruction))
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
                    if (!_globals.TryGetValue(varName, out LoxValue? value))
                    {
                        AddRuntimeError($"Undefined variable '{varName.AsString}'.");
                        return InterpretResult.RuntimeError;
                    }
                    _stack.Push(value);
                    break;
                case SetGlobal:
                    Obj globalName = Frame.ReadConstant().AsObj;
                    if (!_globals.ContainsKey(globalName))
                    {
                        AddRuntimeError($"Undefined variable '{globalName.AsString}'.");
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
                    if (IsFalsey(_stack.Peek(0)))
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
                    if (!CallValue(_stack.Peek(argCount), argCount))
                    {
                        return InterpretResult.RuntimeError;
                    }
                    break;
                case Closure:
                    CreateClosure();
                    break;
                case SetUpValue:
                    byte setUpValueSlot = Frame.ReadByte();
                    Frame.Closure.UpValues[setUpValueSlot].LoxValue.Copy(_stack.Peek(0));
                    break;
                case GetUpValue:
                    byte getUpvalueSlot = Frame.ReadByte();
                    _stack.Push(Frame.Closure.UpValues[getUpvalueSlot].LoxValue);
                    break;
                case CloseUpValue:
                    CloseUpValues(_stack.StackTop - 1);
                    _stack.Pop();
                    break;
                case Class:
                    _stack.Push(LoxValue.Object(Obj.Class(Frame.ReadString(), false)));
                    break;
                case StaticClass:
                    _stack.Push(LoxValue.Object(Obj.Class(Frame.ReadString(), true)));
                    break;
                case GetProperty:
                    if (!GetProp())
                    {
                        return InterpretResult.RuntimeError;
                    }
                    break;
                case SetProperty:
                    if (!SetProp())
                    {
                        return InterpretResult.RuntimeError;
                    }
                    break;
                case Method:
                    DefineMethod(Frame.ReadString());
                    break;
                case OpCode.Invoke:
                    string method = Frame.ReadString();
                    int methodArgCount = Frame.ReadByte();
                    if (!Invoke(method, methodArgCount))
                    {
                        return InterpretResult.RuntimeError;
                    }
                    break;
                case OpCode.Inherit:
                    if (!Inherit())
                    {
                        return InterpretResult.RuntimeError;
                    }
                    break;
                case GetSuper:
                    string supMethodName = Frame.ReadString();
                    ObjClass superClass = _stack.Pop().AsObj.AsClass;
                    if (!BindMethod(superClass, supMethodName))
                    {
                        return InterpretResult.RuntimeError;
                    }
                    break;
                case EmptyArray:
                    LoxValue array = LoxValue.Object(Obj.Arr());
                    _stack.Push(array);
                    break;
                case InitializedArray:
                    byte initCount = Frame.ReadByte();
                    List<LoxValue> initializedValues = new(initCount);
                    for (int i = 0; i < initCount; i++)
                    {
                        initializedValues.Add(_stack.Pop());
                    }
                    initializedValues.Reverse();
                    LoxValue initializedArray = LoxValue.Object(Obj.Arr(initializedValues));
                    _stack.Push(initializedArray);
                    break;
                case DefaultInitializedArray:
                    if (!DefaultInitArray())
                    {
                        return InterpretResult.RuntimeError;
                    }
                    break;
                case ArrayAccess:
                    if (!AccessArray())
                    {
                        return InterpretResult.RuntimeError;
                    }
                    break;
                case ArrayAssign:
                    if (!AssignArray())
                    {
                        return InterpretResult.RuntimeError;
                    }
                    break;
                default:
                    throw new UnreachableException();
            }
        }
    }

    private bool DefaultInitArray()
    {
        LoxValue c = _stack.Pop();
        if (!c.IsNumber || !(c.AsNumber % 1 == 0) || c.AsNumber > 255)
        {
            AddRuntimeError("Initializer count must be an integer number, which is less than 255.");
            return false;
        }

        byte count = (byte)c.AsNumber;
        LoxValue initValue = _stack.Pop();

        List<LoxValue> initValues = new(count);

        for (int i = 0; i < count; i++)
        {
            initValues.Add(LoxValue.FromLoxValue(initValue));
        }

        initValues.Reverse();
        LoxValue initArray = LoxValue.Object(Obj.Arr(initValues));
        _stack.Push(initArray);
        return true;
    }

    private bool AssignArray()
    {
        LoxValue assignValue = _stack.Pop();
        LoxValue indexValue = _stack.Pop();
        LoxValue targetValue = _stack.Pop();

        if (!indexValue.IsNumber || !(indexValue.AsNumber % 1 == 0))
        {
            AddRuntimeError("Index must be an integer.");
            return false;
        }

        int index = (int)indexValue.AsNumber;

        if (!targetValue.IsObj || !targetValue.AsObj.IsType(ObjType.Array))
        {
            AddRuntimeError("Target must be an array.");
            return false;
        }

        ObjArray array = targetValue.AsObj.AsArray;

        if (array.Array.Count <= index)
        {
            // If index is out of bounds, pad the array with Nil values.
            int difference = index - array.Array.Count;
            array.Array.AddRange(Enumerable.Repeat(LoxValue.Nil(), difference + 1));
        }

        array.Array[index] = assignValue;
        _stack.Push(targetValue);

        return true;
    }

    private bool AccessArray()
    {
        int index = (int)_stack.Pop().AsNumber;
        LoxValue arrValue = _stack.Pop();

        if (!arrValue.IsObj || !(arrValue.AsObj.IsType(ObjType.Array) || arrValue.AsObj.IsType(ObjType.String)))
        {
            AddRuntimeError("Can only index into arrays and strings.");
            return false;
        }

        Obj o = arrValue.AsObj;

        if (o.IsType(ObjType.Array))
        {
            ObjArray objArray = o.AsArray;

            if (objArray.Array.Count <= index)
            {
                AddRuntimeError($"Index is out of bounds. Array size: {index}, index: {index}.");
                return false;
            }
            _stack.Push(objArray.Array[index]);
        }
        else
        {
            string val = o.AsString.StringValue;
            if (val.Length <= index)
            {
                AddRuntimeError($"Index is out of bounds. Array size: {index}, index: {index}.");
                return false;
            }
            _stack.Push(LoxValue.Object(Obj.Str(val[index].ToString())));
        }

        return true;
    }

    private bool Inherit()
    {
        LoxValue superclassValue = _stack.Peek(1);

        if (!superclassValue.IsObj || !superclassValue.AsObj.IsType(ObjType.Class) || superclassValue.AsObj.AsClass.IsStatic)
        {
            AddRuntimeError("Superclass must be a (non-static) class.");
            return false;
        }

        ObjClass superclass = superclassValue.AsObj.AsClass;
        ObjClass inheritingClass = _stack.Peek(0).AsObj.AsClass;

        foreach ((string methodName, LoxValue methodVal) in superclass.Methods)
        {
            inheritingClass.Methods[methodName] = methodVal;
        }

        _stack.Pop();
        return true;
    }

    private bool BindMethod(ObjClass superClass, string supMethodName)
    {
        if (!superClass.Methods.TryGetValue(supMethodName, out LoxValue? method))
        {
            AddRuntimeError($"Undefined property: '{supMethodName}'.");
            return false;
        }

        ObjClosure closure = method.AsObj.AsClosure;

        if (closure.Function.IsStatic)
        {
            AddRuntimeError($"Static methods cannot be accessed from instances.");
            return false;
        }

        ObjBoundMethod boundMethod = Obj.BoundMethod(_stack.Peek(0), closure);
        _stack.Pop();
        _stack.Push(LoxValue.Object(boundMethod));
        return true;
    }

    private bool Invoke(string name, int methodArgCount)
    {
        LoxValue receiver = _stack.Peek(methodArgCount);

        if (!receiver.IsObj || !(receiver.AsObj.IsType(ObjType.Class) || receiver.AsObj.IsType(ObjType.Instance)))
        {
            AddRuntimeError("Can only call methods on instances or classes.");
            return false;
        }

        return InvokeFromClass(receiver.AsObj, name, methodArgCount);
    }

    private bool InvokeFromClass(Obj receiver, string name, int methodArgCount)
    {
        bool isReceiverInstance = receiver.IsType(ObjType.Instance);

        ObjClass objClass = isReceiverInstance ? receiver.AsInstance.ObjClass : receiver.AsClass;

        LoxValue? method;
        if (isReceiverInstance)
        {
            // Receiver is an instance
            // Check if the class has a method with the given name, or if the instance has a field 
            if (!(receiver.AsInstance.Fields.TryGetValue(name, out method) || objClass.Methods.TryGetValue(name, out method)))
            {
                AddRuntimeError($"Undefined property: '{name}'.");
                return false;
            }
        }
        else
        {
            // Receiver is a class
            // Check if the class has a method with the given name, and if that method is a static method
            if (!(objClass.Methods.TryGetValue(name, out method) && method.AsObj.AsClosure.Function.IsStatic))
            {
                AddRuntimeError($"Class '{objClass.Name}' has no static method named '{name}'.");
                return false;
            }
        }

        if (method.IsObj && method.AsObj.IsType(ObjType.BoundMethod))
        {
            // If we are calling a bound method, make sure that the method's original receiver is on the stack
            ObjBoundMethod boundMethod = method.AsObj.AsBoundMethod;
            _stack[_stack.StackTop - 1 - methodArgCount] = boundMethod.Receiver;
        }

        if (!(method.IsObj && (method.AsObj.IsType(ObjType.Closure) || method.AsObj.IsType(ObjType.BoundMethod))))
        {
            AddRuntimeError("Can only call functions and classes.");
            return false;
        }

        ObjClosure closure = method.AsObj.IsType(ObjType.Closure) ? method.AsObj.AsClosure : method.AsObj.AsBoundMethod.Method;

        if (!CheckIfCallable(name, isReceiverInstance, objClass, closure))
        {
            return false;
        }

        return CallFn(closure, methodArgCount);
    }

    private bool CheckIfCallable(string name, bool isReceiverInstance, ObjClass objClass, ObjClosure closure)
    {
        if (closure.Function.IsStatic && isReceiverInstance)
        {
            AddRuntimeError($"Static method '{name}' cannot be called on an instance.");
            return false;
        }
        else if (!closure.Function.IsStatic && !isReceiverInstance)
        {
            AddRuntimeError($"Class '{objClass.Name}' has no static method named '{name}'.");
            return false;
        }

        return true;
    }

    private bool SetProp()
    {
        if (_stack.Peek(1).AsObj is not ObjInstance setInstance)
        {
            AddRuntimeError("Only instances have fields.");
            return false;
        }

        setInstance.Fields[Frame.ReadString()] = _stack.Peek(0);
        LoxValue val = _stack.Pop();
        _stack.Pop();
        _stack.Push(val);
        return true;
    }

    private bool GetProp()
    {
        Obj obj = _stack.Peek(0).AsObj;
        string propertyName = Frame.ReadString();

        if (obj is ObjInstance instance)
        {
            if (instance.Fields.TryGetValue(propertyName, out LoxValue? value))
            {
                _stack.Pop();
                _stack.Push(value);
                return true;
            }
            else
            {
                return BindMethod(instance.ObjClass, propertyName);
            }
        }
        else if (obj is ObjClass objClass)
        {
            if (objClass.Methods.TryGetValue(propertyName, out LoxValue? value))
            {
                _stack.Pop();
                _stack.Push(value);
                return true;
            }
            else
            {
                AddRuntimeError($"Class '{objClass.Name}' has no static method named '{propertyName}'.");
                return false;
            }
        }
        else
        {
            AddRuntimeError("Only instances have properties.");
            return false;
        }
    }

    private void DefineMethod(string name)
    {
        LoxValue method = _stack.Peek(0);
        ObjClass objClass = _stack.Peek(1).AsObj.AsClass;
        objClass.Methods[name] = method;
        _stack.Pop();
    }

    private void CloseUpValues(int locationOnStack)
    {
        while (openUpValues is not null && openUpValues.Location >= locationOnStack)
        {
            openUpValues.Closed = openUpValues.LoxValue;
            openUpValues = openUpValues.Next;
        }
    }

    private void CreateClosure()
    {
        ObjFunction function = Frame.ReadConstant().AsObj.AsFunction;

        List<ObjUpValue> upValues = Enumerable.Range(0, function.UpValueCount).Select<int, ObjUpValue>(_ => null!).ToList();
        ObjClosure closure = new() { Function = function, UpValues = upValues };
        _stack.Push(LoxValue.Object(closure));

        for (int i = 0; i < function.UpValueCount; i++)
        {
            bool isLocal = Frame.ReadByte() == 1;
            byte index = Frame.ReadByte();

            closure.UpValues[i] = isLocal ? CaptureValue(Frame.Slot + index) : Frame.Closure.UpValues[index];
        }
    }

    private ObjUpValue CaptureValue(int locationOnStack)
    {
        LoxValue value = _stack[locationOnStack];

        ObjUpValue? prevUpValue = null;
        ObjUpValue? upValue = openUpValues;
        while (upValue is not null)
        {
            prevUpValue = upValue;
            upValue = upValue.Next;
        }

        if (upValue is not null && upValue.LoxValue == value)
        {
            return upValue;
        }

        ObjUpValue createdUpValue = new() { LoxValue = value, Next = upValue, Location = locationOnStack };
        if (prevUpValue is null)
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
        if (c.IsObj)
        {
            Obj callee = c.AsObj;
            switch (callee.Type)
            {
                case ObjType.BoundMethod:
                    ObjBoundMethod boundMethod = callee.AsBoundMethod;
                    _stack[_stack.StackTop - argCount - 1] = boundMethod.Receiver;
                    return CallFn(boundMethod.Method, argCount);
                case ObjType.Class:
                    ObjClass objClass = callee.AsClass;
                    if (objClass.IsStatic)
                    {
                        AddRuntimeError("Static classes cannot be instantiated.");
                        return false;
                    }
                    _stack[_stack.StackTop - argCount - 1] = LoxValue.Object(Obj.Instance(objClass));
                    if (objClass.Methods.TryGetValue(INIT_STRING, out LoxValue? initializer))
                    {
                        return CallFn(initializer.AsObj.AsClosure, argCount);
                    }
                    else if (argCount != 0)
                    {
                        AddRuntimeError($"Expected 0 arguments, but got {argCount}.");
                        return false;
                    }
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
        if (argCount != nativeFn.Arity)
        {
            AddRuntimeError($"Expected {nativeFn.Arity} arguments, but got {argCount}.");
            return false;
        }

        (LoxValue? result, bool success) = nativeFn.Function.Invoke(_stack.StackTop - argCount);
        if (!success)
        {
            return false;
        }

        _stack.StackTop -= argCount + 1;
        _stack.Push(result!);
        return true;
    }

    private bool CallFn(ObjClosure closure, int argCount)
    {
        if (argCount != closure.Function.Arity)
        {
            AddRuntimeError($"Expected {closure.Function.Arity} arguments, but got {argCount}.");
            return false;
        }

        if (frameCount == FRAMES_MAX)
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

        if (op.IsLogicalOp())
        {
            return HandleBool(a, b, op);
        }
        else if (op.IsMathOp() || op.IsComparisonOp())
        {
            if (!(a.IsNumber && b.IsNumber) && op == Add)
            {
                // Concat a and b
                // Automatically convert a non-string value (e.g. a number) to string, when one of the operand is string
                string left = a.ToString();
                string right = b.ToString();

                _stack.Push(LoxValue.Object(Obj.Str(left + right)));
                return true;
            }
            else
            {
                return HandleNum(a, b, op);
            }
        }
        else
        {
            throw new UnreachableException($"Opcode was {op}");
        }
    }

    private bool HandleBool(LoxValue a, LoxValue b, OpCode op)
    {
        if (op == Or)
        {
            if (!IsFalsey(a))
            {
                _stack.Push(a);
                return true;
            }
        }
        else
        {
            if (IsFalsey(a))
            {
                _stack.Push(a);
                return true;
            }
        }

        _stack.Push(b);
        return true;
    }

    private bool HandleNum(LoxValue a, LoxValue b, OpCode op)
    {
        if (!(a.IsNumber && b.IsNumber))
        {
            AddRuntimeError("Operands must be numbers.");
            return false;
        }

        double left = a.AsNumber;
        double right = b.AsNumber;

        if (op.IsComparisonOp())
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
            if (op == Divide && right == 0)
            {
                AddRuntimeError("Attempted division by zero.");
                return false;
            }

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

    private void ResetVm()
    {
        Errors.Clear();
        callFrames = new CallFrame[STACK_MAX];
        openUpValues = null;
        _stack.Reset();
        frameCount = 0;
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

    private void DefineNative(string name, int arity, Func<int, (LoxValue? returnValue, bool success)> native)
    {
        Obj nameObj = LoxValue.Object(name).AsObj;
        ObjNativeFn objNativeFn = new() { Arity = arity, Name = name, Function = native };
        LoxValue natFn = LoxValue.Object(objNativeFn);
        _globals.Add(nameObj, natFn);
    }
}

// TODO: Test performance with class and readonly struct
// This is a mutable value type - try to make it immutable?
internal struct CallFrame
{
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