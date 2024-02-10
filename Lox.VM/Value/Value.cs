namespace LoxVM.Value;

internal struct Value
{
    public double Val { get; private set; }

    public Value(double val)
    {
        Val = val;
    }

    public override readonly string ToString() => $"{Val}";
}

internal class Values
{
    public int Count => _values.Count;
    public Value this[int index] => _values[index];

    private readonly List<Value> _values;

    internal Values()
    {
        _values = [];
    }

    internal void WriteValuesList(Value value) => _values.Add(value);

    internal void FreeValuesList() => _values.Clear();
}
