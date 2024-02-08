using System.Diagnostics;
using Shared;
using static Shared.TokenType;

namespace Frontend.Scanner;

public class Scanner
{
    public bool HadError => errors.Count != 0; 
    public IEnumerable<ScannerError> Errors => errors;

    private readonly string _source;
    private readonly List<Token> tokens = [];
    private int current = 0;
    private int start = 0;
    private int line = 1;

    private readonly List<ScannerError> errors;

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
        {"static", STATIC},
        {"foreach", FOREACH},
        {"in", IN}};
    }

    public Scanner(string source)
    {
        _source = source;
        errors = [];
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
            case ';': AddToken(SEMICOLON); break;
            case '-':
                HandleToken(MINUS);
                break;
            case '+': 
                HandleToken(PLUS);
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
                    Error(line, $"Unexpected character! ({c})");
                }
                break;
        }
    }

    private void HandleToken(TokenType type)
    {
        (TokenType postfixToken, TokenType compoundAssignmentToken) = type switch
        {
            MINUS   => (MINUS_MINUS, MINUS_EQUAL),
            PLUS    => (PLUS_PLUS, PLUS_EQUAL),
            _       => throw new UnreachableException()
        };

        if(Match('='))
        {
            AddToken(compoundAssignmentToken);
        }
        else if(Match('+') || Match('-'))
        {
            AddToken(postfixToken);
        }
        else 
        {
            AddToken(type);
        }
    }

    private void HandleMultilineComment()
    {

        int nestingLevel = 1;
        int startLine = line;

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
            Error(startLine, "Unterminated multiline comment.");
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
            Advance(); // Consume the '.'

            while (IsDigit(Peek()))
            {
                Advance();
            }
        }

        AddToken(NUMBER, double.Parse(_source[start..current]));
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
            Error(line, "Unterminated string.");
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

    private char Advance() => _source[current++];

    private void AddToken(TokenType type) => AddToken(type, null);

    private void AddToken(TokenType type, object? literal)
    {
        string text = _source[start..current];
        tokens.Add(new Token(type, text, literal, line));
    }

    private void Error(int line, string msg) => errors.Add(new(line, msg));
}