using Generated;
using Shared;

namespace Frontend.Test.Parser.TestData;

public sealed class ExpressionTestData : SimpleParserTestData
{
    public ExpressionTestData()
    {
        Add("1 == 1;", ToIEnumerable(new Stmt.Expression(new Expr.Binary(new Expr.Literal(1, new Token(TokenType.NUMBER, "1", 1, 1)), new Token(TokenType.EQUAL_EQUAL, "==", null, 1), new Expr.Literal(1, new Token(TokenType.NUMBER, "1", 1, 1))))));
    }
}

