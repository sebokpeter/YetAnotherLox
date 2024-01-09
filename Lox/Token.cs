namespace Lox;

public class Token {
    readonly TokenType _type;
    readonly string _lexeme;
    readonly object? _literal;
    readonly int _line;

    public Token(TokenType type, string lexeme, object? literal, int line)
    {
        this._type = type;
        this._lexeme = lexeme;
        this._literal = literal;
        this._line = line;
    }

    public string Lexeme => this._lexeme;
    public TokenType Type => this._type;
    public object? Literal => this._literal;
    public int Line => this._line;

    public override string ToString()
    {
        return $"{this._type} {this._lexeme} {this._literal}";
    }
}