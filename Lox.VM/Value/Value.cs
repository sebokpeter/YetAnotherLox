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