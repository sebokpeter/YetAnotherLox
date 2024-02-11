using LoxVM.Chunk;

namespace LoxVM;

internal static class Debug
{
    internal static void PrintStack(this Value.Value[] stack, byte stackTop)
    {
        for(byte slot = 0; slot < stackTop; slot++)
        {
            Console.WriteLine($"\t[{stack[slot]}]");
        }
    }

    internal static void Disassemble(this Chunk.Chunk chunk, string name)
    {
        Console.WriteLine($"== {name} ==");

        for(int offset = 0; offset < chunk.Count;)
        {
            offset = chunk.DisassembleInstruction(offset);
        }
    }

    internal static int DisassembleInstruction(this Chunk.Chunk chunk, int offset)
    {
        Console.Write($"{offset:0000} ");

        if(offset > 0 && chunk.Lines[offset] == chunk.Lines[offset - 1])
        {
            Console.Write("\t| ");
        }
        else
        {
            Console.Write($"{chunk.Lines[offset]:0000} ");
        }

        OpCode opCode = (OpCode)chunk[offset];

        return opCode switch
        {
            OpCode.Return     => SimpleInstruction(opCode, offset),
            OpCode.Constant   => ConstantInstruction(opCode, chunk, offset),
            OpCode.Negate     => SimpleInstruction(opCode, offset),
            OpCode.Add        => SimpleInstruction(opCode, offset),
            OpCode.Subtract   => SimpleInstruction(opCode, offset),
            OpCode.Multiply   => SimpleInstruction(opCode, offset),
            OpCode.Divide     => SimpleInstruction(opCode, offset),
            OpCode.Modulo     => SimpleInstruction(opCode, offset),
            _                 => UnknownInstruction(opCode, offset)
        };
    }

    private static int ConstantInstruction(OpCode opCode, Chunk.Chunk chunk, int offset)
    {
        byte constant = chunk[offset + 1];
        Console.Write($"{opCode, -19} {constant:0000} ");
        Console.WriteLine(chunk.Constants[constant]);
        return offset + 2;
    }

    private static int UnknownInstruction(OpCode opCode, int offset)
    {
        Console.WriteLine($"Unknown instruction: {opCode}");
        return offset + 1;    
    }

    private static int SimpleInstruction(OpCode opCode, int offset)
    {
        Console.WriteLine(opCode.ToString());
        return offset + 1;
    }
}