using LoxVM.Chunk;
using static LoxVM.Chunk.OpCode;

namespace LoxVM;

public class Lox
{
    public static void Main() 
    {
        Chunk.Chunk chunk = new();
        int constant = chunk.AddConstant(new(1.2));
        chunk.WriteChunk(OpConstant);
        chunk.WriteChunk((byte)constant);

        constant = chunk.AddConstant(new(120));
        chunk.WriteChunk(OpConstant);
        chunk.WriteChunk((byte)constant);

        chunk.WriteChunk(OpReturn);

        chunk.DisassembleChunk("Test Chunk");
    }
}