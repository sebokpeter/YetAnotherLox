using FluentAssertions;
using Shared;

namespace Frontend.Test.Scanner;

/// <summary>
/// Unit tests for the <see cref="Scanner">.
/// Test if the scanner can convert a string (the source) into a stream of <see cref="Token"/>s properly.
/// </summary>
public class ScannerTests
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

    // TODO: expand testing to ensure that the scanner properly produces tokens
    // - Error handling 
    // - Test for all constructs
    //      * Variable declarations
    //      * Class declarations
    //      * Function declarations
    //      * Etc.
}