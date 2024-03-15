using BenchmarkDotNet.Attributes;

using Shared;

namespace Lox.Benchmark.Scanner;

[MemoryDiagnoser]
public class ScannerBenchmark
{
    [Params(1, 10, 100, 1_000, 10_000)]
    public int amount;

    private string emptySource = String.Empty;
    private string varSource = String.Empty;

    [GlobalSetup]
    public void GlobalSetup()
    {
        emptySource = new string(' ', amount);
        varSource = String.Join(' ', Enumerable.Range(0, amount).Select(i => $"var {i};"));
    }


    [Benchmark]
    public List<Token> ScanEmpty()
    {
        Frontend.Scanner.Scanner scanner = new(emptySource);
        return scanner.ScanTokens();
    }

    [Benchmark]
    public List<Token> ScanVariables()
    {
        Frontend.Scanner.Scanner scanner = new(varSource);
        return scanner.ScanTokens();
    }
}