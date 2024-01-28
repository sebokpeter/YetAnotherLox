using FluentAssertions;
using Frontend.Test.Scanner.TestData;
using Shared;

namespace Frontend.Test.Scanner;

public class ScannerKeywordsTest
{
    public static TheoryData Data => new KeywordTheoryData() 
    {
        {"and", TokenType.AND}
    };


    [Theory]
    [MemberData(nameof(Data))]
    public void KeywordSource_Returns_IEnumerable_With_CorrectTokenType(string source, TokenType expected)
    {
        Frontend.Scanner.Scanner scanner = new(source);

        IEnumerable<Token> tokens = scanner.ScanTokens();

        tokens.Should().ContainSingle(t => t.Type == expected);
    }
}