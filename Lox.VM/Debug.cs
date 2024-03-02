using System.Reflection.Metadata.Ecma335;

using LoxVM.Chunk;
using LoxVM.Value;

using static LoxVM.Chunk.OpCode;

namespace LoxVM;

internal static class Debug
{
    internal static void PrintStack<T>(this VmStack<T> stack)
    {
        foreach (T loxValue in stack)
        {
            Console.WriteLine($"\t[{loxValue}]");
        }
    }

    internal static void Disassemble(this Chunk.Chunk chunk, string name)
    {
        Console.WriteLine($"== {name} ==");

        for (int offset = 0; offset < chunk.Count;)
        {
            offset = chunk.DisassembleInstruction(offset);
        }
    }

    internal static int DisassembleInstruction(this Chunk.Chunk chunk, int offset)
    {
        Console.Write($"{offset:0000} ");

        if (offset > 0 && chunk.Lines[offset] == chunk.Lines[offset - 1])
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

            Return => SimpleInstruction(opCode, offset),
            Negate => SimpleInstruction(opCode, offset),
            Add => SimpleInstruction(opCode, offset),
            Subtract => SimpleInstruction(opCode, offset),
            Multiply => SimpleInstruction(opCode, offset),
            Divide => SimpleInstruction(opCode, offset),
            Modulo => SimpleInstruction(opCode, offset),
            Nil => SimpleInstruction(opCode, offset),
            True => SimpleInstruction(opCode, offset),
            False => SimpleInstruction(opCode, offset),
            Not => SimpleInstruction(opCode, offset),
            Equal => SimpleInstruction(opCode, offset),
            Less => SimpleInstruction(opCode, offset),
            Greater => SimpleInstruction(opCode, offset),
            And => SimpleInstruction(opCode, offset),
            Or => SimpleInstruction(opCode, offset),
            Print => SimpleInstruction(opCode, offset),
            Pop => SimpleInstruction(opCode, offset),
            CloseUpValue => SimpleInstruction(opCode, offset),
            Inherit => SimpleInstruction(opCode, offset),
            EmptyArray => SimpleInstruction(opCode, offset),
            DefaultInitializedArray => SimpleInstruction(opCode, offset),
            ArrayAccess => SimpleInstruction(opCode, offset),
            ArrayAssign => SimpleInstruction(opCode, offset),

            Constant => ConstantInstruction(opCode, chunk, offset),
            DefineGlobal => ConstantInstruction(opCode, chunk, offset),
            GetGlobal => ConstantInstruction(opCode, chunk, offset),
            SetGlobal => ConstantInstruction(opCode, chunk, offset),
            Class => ConstantInstruction(opCode, chunk, offset),
            StaticClass => ConstantInstruction(opCode, chunk, offset),
            GetProperty => ConstantInstruction(opCode, chunk, offset),
            SetProperty => ConstantInstruction(opCode, chunk, offset),
            Method => ConstantInstruction(opCode, chunk, offset),
            GetSuper => ConstantInstruction(opCode, chunk, offset),

            GetLocal => ByteInstruction(opCode, chunk, offset),
            SetLocal => ByteInstruction(opCode, chunk, offset),
            GetUpValue => ByteInstruction(opCode, chunk, offset),
            SetUpValue => ByteInstruction(opCode, chunk, offset),
            Call => ByteInstruction(opCode, chunk, offset),

            JumpIfFalse => JumpInstruction(opCode, 1, chunk, offset),
            Jump => JumpInstruction(opCode, 1, chunk, offset),
            Loop => JumpInstruction(opCode, -1, chunk, offset),

            Closure => ClosureInstruction(opCode, chunk, offset),

            Invoke => InvokeInstruction(opCode, chunk, offset),

            InitializedArray => InitializedArrayInstruction(opCode, chunk, offset),

            _ => UnknownInstruction(opCode, offset)
        };
    }

    private static int InitializedArrayInstruction(OpCode opCode, Chunk.Chunk chunk, int offset)
    {
        byte initCount = chunk[offset + 1];

        string opString = $"{opCode} ({initCount} values)";

        Console.WriteLine($"{opString,-19}");

        return offset + 2;
    }

    private static int InvokeInstruction(OpCode opCode, Chunk.Chunk chunk, int offset)
    {
        byte constant = chunk[offset + 1];
        byte argCount = chunk[offset + 2];

        string opString = $"{opCode} ({argCount} args)";

        Console.Write($"{opString,-19} {constant:0000} ");
        Console.WriteLine(chunk.Constants[constant]);

        return offset + 3;
    }

    private static int ClosureInstruction(OpCode opCode, Chunk.Chunk chunk, int offset)
    {
        offset++;
        byte constant = chunk[offset++];
        Console.WriteLine($"{opCode,-19} {constant:0000} {chunk.Constants[constant]}");

        ObjFunction function = chunk.Constants[constant].AsObj.AsFunction;
        for (int i = 0; i < function.UpValueCount; i++)
        {
            bool isLocal = chunk[offset++] == 1;
            int index = chunk[offset++];
            Console.WriteLine($"{offset - 2:0000}\t|---------------------{(isLocal ? "local" : "upvalue")} {index}");
        }

        function.Chunk.Disassemble(function.Name);

        Console.WriteLine();

        return offset;
    }

    private static int ConstantInstruction(OpCode opCode, Chunk.Chunk chunk, int offset)
    {
        byte constant = chunk[offset + 1];
        Console.WriteLine($"{opCode,-19} {constant:0000} {chunk.Constants[constant]}");
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

        Console.WriteLine($"{opCode,-19} {offset:0000} -> {offset + 3 + sign * jump}");
        return offset + 3;
    }
}