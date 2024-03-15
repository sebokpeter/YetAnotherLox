using BenchmarkDotNet.Running;

using Lox.Benchmark.Parser;
using Lox.Benchmark.Scanner;

BenchmarkRunner.Run([
    BenchmarkConverter.TypeToBenchmarks(typeof(ScannerBenchmark)),
    BenchmarkConverter.TypeToBenchmarks(typeof(ParserBenchmark))
]);