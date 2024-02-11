using System.Diagnostics;
using LoxVM.Chunk;
using LoxVM;

namespace LoxVM.VM;

internal class Vm : IDisposable
{
    private Chunk.Chunk? chunk;

    private bool disposed;
    private byte ip;

    public Vm()
    {
        disposed = false;
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
                Debug.DisassembleInstruction(chunk, ip);
            #endif

            OpCode instruction = (OpCode)ReadByte();

            switch (instruction)
            {
                case OpCode.OpReturn:
                    return InterpretResult.Ok;
                case OpCode.OpConstant:
                    Value.Value constant = ReadConstant();
                    Console.WriteLine(constant);
                    break;
                default:
                    throw new UnreachableException();
            }
        }
    }

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