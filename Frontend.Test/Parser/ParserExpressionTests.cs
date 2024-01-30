using FluentAssertions;
using Frontend.Test.Parser.TestData;
using Generated;
using Shared;

namespace Frontend.Test.Parser;

public class ParserExpressionTests
{
    [Theory]
    [ClassData(typeof(ExpressionTestData))]
    public void ParseEqualityExpr_ReturnsCorrectAST(string source, IEnumerable<Stmt> expected)
    {
        IEnumerable<Token> tokens = ParserTestUtils.Scan(source);

        Frontend.Parser.Parser parser = new(tokens.ToList());

        IEnumerable<Stmt> ast = parser.Parse();

        parser.HadError.Should().BeFalse();

        ast.Should().BeEquivalentTo(expected, opt => opt.RespectingRuntimeTypes());
    }
}