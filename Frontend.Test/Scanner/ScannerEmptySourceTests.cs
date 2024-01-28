using FluentAssertions;
using Shared;

namespace Frontend.Test.Scanner;

public class ScannerEmptySourceTests
{
    [Fact]
    public void EmptyString_Returns_IEnumerable_Containing_EoFToken()
    {
        string source = String.Empty;
        Frontend.Scanner.Scanner scanner = new(source);

        IEnumerable<Token> tokens = scanner.ScanTokens();

        tokens.Should().ContainSingle(t => t.Line == 1 && t.Type == TokenType.EOF);
    }

    [Theory]
    [InlineData("", 1)]
    [InlineData("\n", 2)]
    [InlineData("\n \n \n", 4)]
    public void EmptyString_MultipleLines_Returns_EoFToken(string source, int line)
    {
        Frontend.Scanner.Scanner scanner = new(source);

        IEnumerable<Token> tokens = scanner.ScanTokens();

        tokens.Should().ContainSingle(t => t.Line == line && t.Type == TokenType.EOF);
    }
}