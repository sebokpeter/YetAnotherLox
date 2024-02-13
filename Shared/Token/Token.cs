namespace Shared;

public class Token {
    readonly TokenType _type;
    readonly string _lexeme;
    readonly object? _literal;
    readonly int _line;

    public Token(TokenType type, string lexeme, object? literal, int line)
    {
        _type = type;
        _lexeme = lexeme;
        _literal = literal;
        _line = line;
    }

    public string Lexeme => _lexeme;
    public TokenType Type => _type;
    public object? Literal => _literal;
    public int Line => _line;

    public override string ToString()
    {
        return $"{_type} {_lexeme} {_literal}";
    }
}