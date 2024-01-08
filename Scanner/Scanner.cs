using static LoxConsole.TokenType;

namespace LoxConsole.Scanner;

class Scanner
{
    private readonly string _source;
    private readonly List<Token> tokens = [];
    private int current = 0;
    private int start = 0;
    private int line = 1;

    private static readonly Dictionary<string, TokenType> keywords;

    static Scanner()
    {
        keywords = new() {
        {"and", AND},
        {"class", CLASS},
        {"else", ELSE},
        {"false", FALSE},
        {"for", FOR},
        {"fun", FUN},
        {"if", IF},
        {"nil", NIL},
        {"or", OR},
        {"print", PRINT},
        {"return", RETURN},
        {"super", SUPER},
        {"this", THIS},
        {"true", TRUE},
        {"var", VAR},
        {"while", WHILE},
        {"break", BREAK},
        {"continue", CONTINUE}};
    }

    public Scanner(string source)
    {
        _source = source;
    }

    public List<Token> ScanTokens()
    {
        while (!IsAtEnd())
        {
            start = current;
            ScanToken();
        }

        tokens.Add(new Token(EOF, "", null, line));
        return tokens;
    }

    private bool IsAtEnd()
    {
        return current >= _source.Length;
    }

    private void ScanToken()
    {
        char c = Advance();
        switch (c)
        {
            case '(': AddToken(LEFT_PAREN); break;
            case ')': AddToken(RIGHT_PAREN); break;
            case '{': AddToken(LEFT_BRACE); break;
            case '}': AddToken(RIGHT_BRACE); break;
            case '[': AddToken(LEFT_SQUARE); break;
            case ']': AddToken(RIGHT_SQUARE); break;
            case ',': AddToken(COMMA); break;
            case '.': AddToken(DOT); break;
            case '-': AddToken(MINUS); break;
            case '+': AddToken(PLUS); break;
            case ';': AddToken(SEMICOLON); break;
            case '*': AddToken(STAR); break;
            case '%': AddToken(MODULO); break;
            case '!':
                AddToken(Match('=') ? BANG_EQUAL : BANG);
                break;
            case '=':
                AddToken(Match('=') ? EQUAL_EQUAL : EQUAL);
                break;
            case '<':
                AddToken(Match('=') ? LESS_EQUAL : LESS);
                break;
            case '>':
                AddToken(Match('=') ? GREATER_EQUAL : GREATER);
                break;
            case '/':
                if(Match('/'))
                {
                    while(Peek() != '\n' && !IsAtEnd())
                    {
                        Advance();
                    }
                }
                else if(Match('*'))
                {
                    while(Peek() != '*' && ! IsAtEnd())
                    {
                        if(Peek() == '\n')
                        {
                            line++;
                        }

                        Advance();
                    }
                    // Consume '*'
                    Advance();
                    if(!Match('/'))
                    {
                        Lox.Error(line, "Expect '/' to terminate multiline comment.");
                    }
                }
                else
                {
                    AddToken(SLASH);
                }
                break;
            case ' ':
            case '\r':
            case '\t':
                // Ignore whitespace
                break;
            case '\n':
                line++;
                break;
            case '"':
                String();
                break;
            default:
                if (IsDigit(c))
                {
                    Number();
                }
                else if (IsAlpha(c))
                {
                    Identifier();
                }
                else
                {
                    Lox.Error(line, $"Unexpected character! ({c})");
                }
                break;
        }
    }

    private void Identifier()
    {
        while (IsAlphaNumeric(Peek()))
        {
            Advance();
        }

        string text = _source[start..current];
        if (keywords.TryGetValue(text, out TokenType type))
        {
            AddToken(type);
        }
        else
        {
            AddToken(IDENTIFIER);
        }
    }

    private static bool IsAlpha(char c)
    {
        return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_';
    }

    private static bool IsAlphaNumeric(char c)
    {
        return IsAlpha(c) || IsDigit(c);
    }

    private void Number()
    {
        while (IsDigit(Peek()))
        {
            Advance();
        }

        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            Advance(); // Consume the .

            while (IsDigit(Peek()))
            {
                Advance();
            }
        }

        AddToken(NUMBER, double.Parse(_source.Substring(start, current - start)));
    }

    private char PeekNext()
    {
        if (current + 1 >= _source.Length)
        {
            return '\0';
        }
        return _source[current + 1];
    }

    private static bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    private void String()
    {
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\n')
            {
                line++;
            }
            Advance();
        }

        if (IsAtEnd())
        {
            Lox.Error(line, "Unterminated string");
            return;
        }

        // Closing '"'
        Advance();

        string value = _source.Substring(start + 1, current - start - 2);
        AddToken(STRING, value);
    }

    private char Peek()
    {
        if (IsAtEnd())
        {
            return '\n';
        }
        return _source[current];
    }

    private bool Match(char expected)
    {
        if (IsAtEnd())
        {
            return false;
        }
        if (_source[current] != expected)
        {
            return false;
        }
        current++;
        return true;
    }

    private char Advance()
    {
        return _source[current++];
    }

    private void AddToken(TokenType type)
    {
        AddToken(type, null);
    }

    private void AddToken(TokenType type, object? literal)
    {
        string text = _source[start..current];
        tokens.Add(new Token(type, text, literal, line));
    }
}