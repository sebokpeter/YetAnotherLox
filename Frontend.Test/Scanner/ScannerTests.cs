using FluentAssertions;
using Shared;
using static Shared.TokenType;

namespace Frontend.Test.Scanner;

public class ScannerTests
{

    [Theory]
    [InlineData("// ", 1)]
    [InlineData("// This is a comment \n", 2)]
    public void ScanComment_Returns_EoF(string source, int line)
    {
        Frontend.Scanner.Scanner scanner = new(source);

        IEnumerable<Token> tokens = scanner.ScanTokens();

        tokens.Should().HaveCount(1);
        tokens.Should().ContainSingle(t => t.Type == EOF).Which.Line.Should().Be(line);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("100000", 100_000)]
    [InlineData("1.2", 1.2)]
    [InlineData("99999.99999", 99999.99999)]
    public void ScanNumbers_Returns_LiteralToken(string source, double number)
    {
        Frontend.Scanner.Scanner scanner = new(source);

        IEnumerable<Token> tokens = scanner.ScanTokens();

        tokens.Should().HaveCount(2);
        tokens.Should().ContainSingle(t => t.Type == NUMBER).Which.Literal.Should().BeOfType<double>().Which.Should().Be(number);
    }

    [Theory]
    [InlineData("++", PLUS_PLUS)]
    [InlineData("--", MINUS_MINUS)]
    public void ScanPostfix_Returns_PostfixToken(string source, TokenType expected)
    {
        Frontend.Scanner.Scanner scanner = new(source);

        IEnumerable<Token> tokens = scanner.ScanTokens();

        tokens.Should().HaveCount(2);
        tokens.Should().ContainSingle(t => t.Type == expected);
    }

    [Theory]
    [InlineData("+=", PLUS_EQUAL)]
    [InlineData("-=", MINUS_EQUAL)]
    [InlineData("*=", STAR_EQUAL)]
    [InlineData("/=", SLASH_EQUAL)]
    [InlineData("%=", MODULO_EQUAL)]
    public void ScanCompoundAssignment_Returns_CompoundAssignmentToken(string source, TokenType expected)
    {
        Frontend.Scanner.Scanner scanner = new(source);

        IEnumerable<Token> tokens = scanner.ScanTokens();

        tokens.Should().HaveCount(2);
        tokens.Should().ContainSingle(t => t.Type == expected);
    }

    [Theory]
    [InlineData("==", EQUAL_EQUAL)]
    [InlineData("<", LESS)]
    [InlineData("<=", LESS_EQUAL)]
    [InlineData(">", GREATER)]
    [InlineData(">=", GREATER_EQUAL)]
    [InlineData("!=", BANG_EQUAL)]
    public void ScanComparison_Returns_ComparisonToken(string source, TokenType expected)
    {
        Frontend.Scanner.Scanner scanner = new(source);

        IEnumerable<Token> tokens = scanner.ScanTokens();

        tokens.Should().HaveCount(2);
        tokens.Should().ContainSingle(t => t.Type == expected);
    }

    [Theory]
    [InlineData("+", PLUS)]
    [InlineData("-", MINUS)]
    [InlineData("*", STAR)]
    [InlineData("/", SLASH)]
    [InlineData("%", MODULO)]
    public void ScanBinaryOperators_Returns_BinOpToken(string source, TokenType expected)
    {
        Frontend.Scanner.Scanner scanner = new(source);

        IEnumerable<Token> tokens = scanner.ScanTokens();

        tokens.Should().HaveCount(2);
        tokens.Should().ContainSingle(t => t.Type == expected);
    }

    [Theory]
    [InlineData("(", LEFT_PAREN)]
    [InlineData(")", RIGHT_PAREN)]
    [InlineData("{", LEFT_BRACE)]
    [InlineData("}", RIGHT_BRACE)]
    [InlineData("[", LEFT_SQUARE)]
    [InlineData("]", RIGHT_SQUARE)]
    [InlineData(",", COMMA)]
    [InlineData(".", DOT)]
    [InlineData(";", SEMICOLON)]
    public void ScanSingle_Returns_CorrectToken(string source, TokenType expected)
    {
        Frontend.Scanner.Scanner scanner = new(source);

        IEnumerable<Token> tokens = scanner.ScanTokens();

        tokens.Should().HaveCount(2);
        tokens.Should().ContainSingle(t => t.Type == expected);
    }

    [Theory]
    [InlineData("!", BANG)]
    public void ScanUnary_Returns_CorrectToken(string source, TokenType expected)
    {
        Frontend.Scanner.Scanner scanner = new(source);

        IEnumerable<Token> tokens = scanner.ScanTokens();

        tokens.Should().HaveCount(2);
        tokens.Should().ContainSingle(t => t.Type == expected);
    }

    [Theory]
    [InlineData("\"Single line string\"", "Single line string", 1)]
    [InlineData("\"Multi-line \n string\"", "Multi-line \n string", 2)]
    public void ScanString_Returns_StringToken(string source, string literal, int eofLine)
    {
        Frontend.Scanner.Scanner scanner = new(source);

        IEnumerable<Token> tokens = scanner.ScanTokens();

        tokens.Should().HaveCount(2);
        tokens.Should().ContainSingle(t => t.Type == STRING).Which.Literal.Should().BeOfType<string>().And.Be(literal);
        tokens.Should().ContainSingle(t => t.Type == EOF).Which.Line.Should().Be(eofLine);
    }
}