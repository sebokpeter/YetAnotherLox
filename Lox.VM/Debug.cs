using System.Security.Cryptography;
using LoxVM.Chunk;

namespace LoxVM;

internal static class Debug
{
    internal static void DisassembleChunk(this Chunk.Chunk chunk, string name)
    {
        Console.WriteLine($"== {name} ==");

        for(int offset = 0; offset < chunk.Count;)
        {
            offset = DisassembleInstruction(chunk, offset);
        }
    }

    internal static int DisassembleInstruction(Chunk.Chunk chunk, int offset)
    {
        Console.Write($"{offset:0000} ");

        OpCode opCode = chunk[offset];

        return opCode switch
        {
            OpCode.OpReturn => SimpleInstruction(opCode, offset),
            _ => UnknownInstruction(opCode, offset)
        };
    }

    private static int UnknownInstruction(OpCode opCode, int offset)
    {
        Console.WriteLine($"Unknown instruction: {opCode}");
        return ++offset;    
    }

    private static int SimpleInstruction(OpCode opCode, int offset)
    {
        Console.WriteLine(opCode.ToString());
        return ++offset;
    }
}