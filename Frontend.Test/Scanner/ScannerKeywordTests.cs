using FluentAssertions;
using Frontend.Test.Scanner.TestData;
using Shared;
using static Shared.TokenType;

namespace Frontend.Test.Scanner;

public class ScannerKeywordsTest
{
    public static TheoryData Keywords => new KeywordTheoryData() 
    {
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
        {"in", IN}
    };


    [Theory]
    [MemberData(nameof(Keywords))]
    public void KeywordSource_Returns_IEnumerable_With_CorrectTokenType(string source, TokenType expected)
    {
        Frontend.Scanner.Scanner scanner = new(source);

        IEnumerable<Token> tokens = scanner.ScanTokens();

        tokens.Should().HaveCount(2);
        tokens.Should().ContainSingle(t => t.Type == expected);
        tokens.Should().ContainSingle(t => t.Type == EOF);
    }
}