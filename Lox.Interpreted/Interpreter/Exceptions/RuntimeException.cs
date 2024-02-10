using Shared;

namespace Lox.Interpreter;

[Serializable]
internal class RuntimeException : Exception
{
    private readonly Token? token;

    public Token Token => token!;

    public RuntimeException(Token token, string message) : base(message)
    {
        this.token = token;
    }
}