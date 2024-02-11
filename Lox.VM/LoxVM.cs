using LoxVM.Chunk;
using static LoxVM.Chunk.OpCode;

namespace LoxVM;

public class Lox
{
    public static void Main() 
    {
        Chunk.Chunk chunk = new();
        int constant = chunk.AddConstant(new(1.2));
        chunk.WriteChunk(OpConstant, 1);
        chunk.WriteChunk((byte)constant, 1);

        constant = chunk.AddConstant(new(120));
        chunk.WriteChunk(OpConstant, 2);
        chunk.WriteChunk((byte)constant, 2);

        chunk.WriteChunk(OpReturn, 2);

        chunk.DisassembleChunk("Test Chunk");
    }
}