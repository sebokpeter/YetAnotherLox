using static LoxVM.Chunk.OpCode;

namespace LoxVM;

public class Lox
{
    public static void Main() 
    {
        using VM.Vm vm = new();

        Chunk.Chunk chunk = new();
        int constant = chunk.AddConstant(new(1.2));
        chunk.WriteChunk(Constant, 1);
        chunk.WriteChunk((byte)constant, 1);

        constant = chunk.AddConstant(new(120));
        chunk.WriteChunk(Constant, 2);
        chunk.WriteChunk((byte)constant, 2);

        chunk.WriteChunk(Negate, 2);

        chunk.WriteChunk(Return, 3);

        //chunk.DisassembleChunk("Test Chunk");

        vm.Interpret(chunk);
    }
}