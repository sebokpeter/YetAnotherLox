namespace LoxConsole.Interpreter;

[Serializable]
internal class RuntimeException : Exception
{
    private readonly Token? token;

    public Token Token => token!;

    public RuntimeException()
    {
    }

    public RuntimeException(string? message) : base(message)
    {
    }

    public RuntimeException(Token token, string message) : base(message)
    {
        this.token = token;
    }

    public RuntimeException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}