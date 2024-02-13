namespace LoxVM.Chunk;

internal class Chunk
{
    internal int Count => _code.Count;

    internal List<int> Lines => _lines;

    internal List<Value.Value> Constants => _constants;
    internal byte this[int index] => _code[index];

    private readonly List<int> _lines;
    private readonly List<byte> _code;
    private readonly List<Value.Value> _constants;

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

    internal int AddConstant(Value.Value value)
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
    Modulo
}