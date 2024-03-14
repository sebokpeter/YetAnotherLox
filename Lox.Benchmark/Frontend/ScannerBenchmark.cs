using BenchmarkDotNet.Attributes;

using Shared;

namespace Lox.Benchmark.Scanner;

[MemoryDiagnoser]
public class ScannerBenchmark
{

    [Benchmark]
    [ArgumentsSource(nameof(EmptySource))]
    public List<Token> ScanEmpty(string source)
    {
        Frontend.Scanner.Scanner scanner = new(source);
        return scanner.ScanTokens();
    }

    [Benchmark]
    [ArgumentsSource(nameof(VariableSource))]
    public List<Token> ScanVariables(string source)
    {
        Frontend.Scanner.Scanner scanner = new(source);
        return scanner.ScanTokens();
    }

    public IEnumerable<string> EmptySource()
    {
        yield return "";
        yield return " ";
        yield return new string(' ', 10);
        yield return new string(' ', 100);
        yield return new string(' ', 1_000);
        yield return new string(' ', 10_000);
    }

    public IEnumerable<string> VariableSource()
    {
        yield return "var x;";
        yield return String.Join(' ', Enumerable.Range(0, 10).Select(i => $"var {i};"));
        yield return String.Join(' ', Enumerable.Range(0, 100).Select(i => $"var {i};"));
        yield return String.Join(' ', Enumerable.Range(0, 1_000).Select(i => $"var {i};"));
        yield return String.Join(' ', Enumerable.Range(0, 10_000).Select(i => $"var {i};"));

    }
}