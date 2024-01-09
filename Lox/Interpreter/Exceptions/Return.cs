namespace Lox.Interpreter;

[Serializable]
internal class Return : Exception
{
    internal readonly object? value;

    public Return(object value) : base(null, null)
    {
        this.value = value;
    }
}