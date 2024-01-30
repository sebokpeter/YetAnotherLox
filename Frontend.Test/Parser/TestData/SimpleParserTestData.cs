using Generated;

namespace Frontend.Test.Parser.TestData;

public class SimpleParserTestData : TheoryData
{
    public void Add(string source, IEnumerable<Stmt> expected)
    {
        AddRow(source, expected);
    }

    internal static IEnumerable<Stmt> ToIEnumerable(params Stmt[] stmts) => stmts.AsEnumerable();
}