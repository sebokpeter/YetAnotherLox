using LoxVM.Chunk;
using LoxVM.Value;

namespace LoxVM;

internal static class Debug
{
    internal static void PrintStack(this ValueStack stack)
    {
        foreach (LoxValue loxValue in stack)
        {
            Console.WriteLine($"\t[{loxValue}]");
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
            OpCode.Return       => SimpleInstruction(opCode, offset),
            OpCode.Constant     => ConstantInstruction(opCode, chunk, offset),
            OpCode.Negate       => SimpleInstruction(opCode, offset),
            OpCode.Add          => SimpleInstruction(opCode, offset),
            OpCode.Subtract     => SimpleInstruction(opCode, offset),
            OpCode.Multiply     => SimpleInstruction(opCode, offset),
            OpCode.Divide       => SimpleInstruction(opCode, offset),
            OpCode.Modulo       => SimpleInstruction(opCode, offset),
            OpCode.Nil          => SimpleInstruction(opCode, offset),
            OpCode.True         => SimpleInstruction(opCode, offset),
            OpCode.False        => SimpleInstruction(opCode, offset),
            OpCode.Not          => SimpleInstruction(opCode, offset),
            OpCode.Equal        => SimpleInstruction(opCode, offset),
            OpCode.Less         => SimpleInstruction(opCode, offset),
            OpCode.Greater      => SimpleInstruction(opCode, offset),
            OpCode.And          => SimpleInstruction(opCode, offset),
            OpCode.Or           => SimpleInstruction(opCode, offset),
            OpCode.Print        => SimpleInstruction(opCode, offset),
            OpCode.Pop          => SimpleInstruction(opCode, offset),
            OpCode.DefineGlobal => ConstantInstruction(opCode, chunk, offset),
            OpCode.GetGlobal    => ConstantInstruction(opCode, chunk, offset),
            OpCode.SetGlobal    => ConstantInstruction(opCode, chunk, offset),
            OpCode.GetLocal     => ByteInstruction(opCode, chunk, offset),
            OpCode.SetLocal     => ByteInstruction(opCode, chunk, offset),
            OpCode.JumpIfFalse  => JumpInstruction(opCode, 1, chunk, offset),
            OpCode.Jump         => JumpInstruction(opCode, 1, chunk, offset),
            OpCode.Loop         => JumpInstruction(opCode, -1, chunk, offset),
            _ => UnknownInstruction(opCode, offset)
        };
    }

    private static int ConstantInstruction(OpCode opCode, Chunk.Chunk chunk, int offset)
    {
        byte constant = chunk[offset + 1];
        Console.Write($"{opCode,-19} {constant:0000} ");
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
        Console.WriteLine(opCode);
        return offset + 1;
    }

    private static int ByteInstruction(OpCode opCode, Chunk.Chunk chunk, int offset)
    {
        byte slot = chunk[offset + 1];
        Console.WriteLine($"{opCode,-19} {slot:0000} ");
        return offset + 2;
    }

    private static int JumpInstruction(OpCode opCode, int sign, Chunk.Chunk chunk, int offset)
    {
        ushort jump = (ushort)(chunk[offset + 1] << 8);
        jump |= chunk[offset + 2];

        Console.WriteLine($"{opCode,-19} {offset:0000} -> {offset+3+sign*jump}");
        return offset + 3;
    }
}