namespace LoxVM.Chunk;

internal class Chunk
{
    internal int Count => _code.Count;

    internal List<int> Lines => _lines;

    internal List<Value.LoxValue> Constants => _constants;
    internal byte this[int index] 
    {
        get => _code[index];
        set => _code[index] = value;
    }

    private readonly List<int> _lines;
    private readonly List<byte> _code;
    private readonly List<Value.LoxValue> _constants;

    internal Chunk()
    {
        _code = [];
        _lines = [];
        _constants = [];
    }

    internal void WriteChunk(byte data, int line)
    {
        _code.Add(data);
        _lines.Add(line);
    }
    internal void WriteChunk(OpCode opCode, int line)
    {
        _code.Add((byte)opCode);
        _lines.Add(line);
    }

    internal void FreeChunk()
    {
        _code.Clear();
        _lines.Clear();
        _constants.Clear();
    }

    internal int AddConstant(Value.LoxValue value)
    {
        _constants.Add(value);
        return _constants.Count - 1;
    }
}

internal enum OpCode : byte
{
    Return,
    Constant,
    Negate,
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    True,
    False,
    And,
    Or,
    Not,
    Equal,
    Greater,
    Less,
    Nil,
    Print,
    Pop,
    DefineGlobal,
    GetGlobal,
    SetGlobal,
    GetLocal,
    SetLocal,
    JumpIfFalse,
    Jump,
    Loop,
    Call,
    Closure,
    GetUpValue,
    SetUpValue,
    CloseUpValue,
}

internal static class OpcodeExtensions
{
    internal static bool IsComparisonOp(this OpCode opCode) => opCode == OpCode.Equal || opCode == OpCode.Less || opCode == OpCode.Greater;
    internal static bool IsMathOp(this OpCode opCode)       => opCode == OpCode.Add || opCode == OpCode.Subtract || opCode == OpCode.Multiply || opCode == OpCode.Divide || opCode == OpCode.Modulo;
    internal static bool IsUnaryOp(this OpCode opCode)      => opCode == OpCode.Not || opCode == OpCode.Negate; 
    internal static bool IsLogicalOp(this OpCode opCode)    => opCode == OpCode.And || opCode == OpCode.Or;
}