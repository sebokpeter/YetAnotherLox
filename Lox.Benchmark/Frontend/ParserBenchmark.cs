using BenchmarkDotNet.Attributes;

using Generated;

using Shared;

namespace Lox.Benchmark.Parser;

[MemoryDiagnoser]
public class ParserBenchmark
{
    [Params(1, 10, 100, 1_000, 10_000)]
    public int variableNumber;

    private List<Token> variables = default!;
    private List<Token> sums = default!;


    [GlobalSetup]
    public void GlobalSetup()
    {
        string vars = String.Join(' ', Enumerable.Range(0, variableNumber).Select(i => $"var {i};"));
        Frontend.Scanner.Scanner scanner = new(vars);
        variables = scanner.ScanTokens();

        string sum = String.Join(' ', Enumerable.Range(0, variableNumber).Select(i => $"var i_{i} = {i} + {i};"));
        Frontend.Scanner.Scanner sumScanner = new(sum);
        sums = sumScanner.ScanTokens();
    }

    [Benchmark]
    public List<Stmt> ParseVariablesBenchmark()
    {
        Frontend.Parser.Parser parser = new(variables);
        return parser.Parse();
    }

    [Benchmark]
    public List<Stmt> ParseSumsBenchmark()
    {
        Frontend.Parser.Parser parser = new(sums);
        return parser.Parse();
    }
}