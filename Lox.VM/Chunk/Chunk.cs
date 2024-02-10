namespace LoxVM.Chunk;

internal class Chunk
{
    internal int Count => _code.Count;
    internal Value.Values Constants => _constants;
    internal byte this[int index] => _code[index];


    private readonly List<byte> _code;
    private readonly Value.Values _constants;

    internal Chunk()
    {
        _code = [];
        _constants = new();
    }

    internal void WriteChunk(byte data) => _code.Add(data);
    internal void WriteChunk(OpCode opCode) => _code.Add((byte)opCode);

    internal void FreeChunk()
    {
        _code.Clear();
        _constants.FreeValuesList();
    }

    internal int AddConstant(Value.Value value)
    {
        _constants.WriteValuesList(value);
        return _constants.Count - 1;
    }
}

internal enum OpCode : byte
{
    OpReturn,
    OpConstant,
}