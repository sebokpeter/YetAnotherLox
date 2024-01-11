using static Lox.TokenType;

namespace Lox.Scanner;

class Scanner
{
    private readonly string _source;
    private readonly List<Token> tokens = [];
    private int _current = 0;
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
        {"continue", CONTINUE},
        {"static", STATIC}};

    }

    public Scanner(string source)
    {
        _source = source;
    }

    public List<Token> ScanTokens()
    {
        while (!IsAtEnd())
        {
            start = _current;
            ScanToken();
        }

        tokens.Add(new Token(EOF, "", null, line));
        return tokens;
    }

    private bool IsAtEnd()
    {
        return _current >= _source.Length;
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
            case ';': AddToken(SEMICOLON); break;
            case '-':
                AddToken(Match('=')? MINUS_EQUAL : MINUS);
                break;
            case '+': 
                AddToken(Match('=')? PLUS_EQUAL : PLUS);
                break;
            case '*': 
                AddToken(Match('=')? STAR_EQUAL : STAR);
                break;
            case '%': 
                AddToken(Match('=')? MODULO_EQUAL : MODULO);
                break;
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
                    while (Peek() != '\n' && !IsAtEnd())
                    {
                        Advance();
                    }
                }
                else if(Match('*'))
                {
                    HandleMultilineComment();
                }
                else if(Match('='))
                {
                    AddToken(SLASH_EQUAL);
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

    private void HandleMultilineComment()
    {

        int nestingLevel = 1;

        // This will try to consume all '/*' '*/' pairs. If it gets to the end, and the nesting level is greater than 0, then there is 
        // at least one opening '/*' that is not matched.
        // However, if there is extra closing '*/'s, they will not be matched, and the parser will report an error at a later stage,
        // when it tries to parse them

        while(nestingLevel > 0 && !IsAtEnd())
        {
            if(Peek() == '\n')
            {
                line++;
                Advance();
            }
            else if(Peek() == '/' && PeekNext() == '*')
            {
                nestingLevel++;
                Advance();
                Advance();
            }
            else if(Peek() == '*' && PeekNext() == '/')
            {
                nestingLevel--;
                Advance();
                Advance();
            }
            else 
            {
                Advance();
            }
        }

        if(nestingLevel > 0)
        {
            Lox.Error(line, "Unterminated multiline comment.");
        }
    }

    private void Identifier()
    {
        while (IsAlphaNumeric(Peek()))
        {
            Advance();
        }

        string text = _source[start.._current];
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
            Advance(); // Consume the '.'

            while (IsDigit(Peek()))
            {
                Advance();
            }
        }

        AddToken(NUMBER, double.Parse(_source.Substring(start, _current - start)));
    }

    private char PeekNext()
    {
        if (_current + 1 >= _source.Length)
        {
            return '\0';
        }
        return _source[_current + 1];
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

        string value = _source.Substring(start + 1, _current - start - 2);
        AddToken(STRING, value);
    }

    private char Peek()
    {
        if (IsAtEnd())
        {
            return '\n';
        }
        return _source[_current];
    }

    private bool Match(char expected)
    {
        if (IsAtEnd())
        {
            return false;
        }
        if (_source[_current] != expected)
        {
            return false;
        }
        _current++;
        return true;
    }

    private char Advance()
    {
        return _source[_current++];
    }


    private void AddToken(TokenType type)
    {
        AddToken(type, null);
    }

    private void AddToken(TokenType type, object? literal)
    {
        string text = _source[start.._current];
        tokens.Add(new Token(type, text, literal, line));
    }
}