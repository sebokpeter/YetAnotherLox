namespace LoxVM.Chunk;

internal class Chunk
{
    internal int Count => _code.Count;
    internal OpCode this[int index] => (OpCode)_code[index];

    private readonly List<byte> _code;

    internal Chunk()
    {
        _code = [];
    }

    internal void WriteChunk(OpCode opCode) => _code.Add((byte)opCode);

    internal void FreeChunk() => _code.Clear();
    
}

internal enum OpCode : byte
{
    OpReturn,
}