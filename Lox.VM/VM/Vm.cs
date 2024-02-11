using System.Diagnostics;
using LoxVM.Chunk;

namespace LoxVM.VM;

internal class Vm : IDisposable
{
    private const int STACK_MAX = 256;

    private Chunk.Chunk? chunk;

    private bool disposed;
    private byte ip;

    private readonly Value.Value[] _stack;
    private byte stackTop;

    public Vm()
    {
        disposed = false;
        _stack = new Value.Value[STACK_MAX];
        stackTop = 0;
    }

    internal InterpretResult Interpret(Chunk.Chunk chunk)
    {
        this.chunk = chunk;
        ip = 0;
        return Run();
    }

    private InterpretResult Run()
    {
        if(chunk is null)
        {
            throw new Exception("Chunk is null."); // TODO: Create exception type
        }

        while(true)
        {
            #if DEBUG_TRACE_EXECUTION
                _stack.PrintStack(stackTop);
                Debug.DisassembleInstruction(chunk, ip);
            #endif

            OpCode instruction = (OpCode)ReadByte();

            switch (instruction)
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
                default:
                    throw new UnreachableException();
            }
        }
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

        }

        disposed = true;
    }
}

internal enum InterpretResult
{
    Ok,
    CompileError,
    RuntimeError
}