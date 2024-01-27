using FluentAssertions;
using Shared;

namespace Frontend.Test.Scanner;

public class ScannerErrorHandlingTests
{
    [Theory]
    [InlineData("&")]
    [InlineData("@")]
    public void SingleInvalidCharacter_Returns_Error(string source) 
    {
        Frontend.Scanner.Scanner scanner = new(source);

        IEnumerable<Token> tokens = scanner.ScanTokens();

        tokens.Should().ContainSingle(t => t.Type == TokenType.EOF);
        scanner.HadError.Should().BeTrue();
        scanner.Errors.Should().ContainSingle(err => err.Line == 1 && err.Message == $"Unexpected character! ({source})");
    }

    [Theory]
    [InlineData("var @varname = 1;", '@')]
    [InlineData("class &class {}", '&')]
    public void InvalidCharacterInSource_Returns_Error(string source, char invalidChar)
    {
        Frontend.Scanner.Scanner scanner = new(source);

        scanner.ScanTokens();

        scanner.HadError.Should().BeTrue();
        scanner.Errors.Should().ContainSingle(err => err.Line == 1 && err.Message == $"Unexpected character! ({invalidChar})");
    }

    [Theory]
    [InlineData("/*/* */", 1)]
    [InlineData("/* /* /* */ */ ", 1)]
    [InlineData("\n/* ", 2)]
    [InlineData("\n \n /* \n \n /* \n */", 3)]
    public void UnterminatedMultilineComment_Returns_Error_WithBeginningLineNumber(string source, int line) 
    {
        Frontend.Scanner.Scanner scanner = new(source);

        scanner.ScanTokens();
        scanner.HadError.Should().BeTrue();
        scanner.Errors.Should().ContainSingle(err => err.Line == line && err.Message == $"Unterminated multiline comment.");
    }

    [Theory]
    [InlineData("\"This string is unterminated", 1)]
    [InlineData("\"This \n is \n a \n multiline unterminated string", 4)]
    public void UnterminatedString_Returns_Error(string source, int line) 
    {
        Frontend.Scanner.Scanner scanner = new(source);

        scanner.ScanTokens();
        scanner.HadError.Should().BeTrue();

        scanner.Errors.Should().ContainSingle(err => err.Line == line && err.Message == $"Unterminated string.");
    }
}