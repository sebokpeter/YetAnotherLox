using LoxVM.Chunk;

namespace LoxVM;

public class Lox
{
    public static void Main() 
    {
        Chunk.Chunk chunk = new();
        chunk.WriteChunk(OpCode.OpReturn);
        chunk.WriteChunk(OpCode.OpReturn);

        chunk.DisassembleChunk("Test Chunk");
    }
}