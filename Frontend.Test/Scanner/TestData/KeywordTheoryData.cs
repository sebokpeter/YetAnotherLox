using Shared;

namespace Frontend.Test.Scanner.TestData;

public sealed class KeywordTheoryData : TheoryData
{
    public void Add(string source, TokenType expectedType)
    {
        base.AddRow(source, expectedType);
    }
}