using System.Collections.Concurrent;
using System.Diagnostics;
using E2E.Test;

namespace E2E;

public class Program
{
    public static async Task Main()
    {
        IEnumerable<TestSuite> tests = CreateTests("Lox.E2E/scripts");

        await RunTest(tests);
    }

    private static async Task RunTest(IEnumerable<TestSuite> tests)
    {
        ConcurrentBag<TestSuite> testSuites = new(tests);

        Stopwatch sw = Stopwatch.StartNew();
        await Parallel.ForEachAsync(testSuites, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (testSuite, token) =>
        {
            await testSuite.Run();
        });
        sw.Stop();

        foreach(TestSuite test in testSuites)
        {
            test.ReportTestsResult();
        }

        Console.WriteLine($"\nTests took {sw.ElapsedMilliseconds} milliseconds.");

        DisplayOverallResult(testSuites);
    }

    private static void DisplayOverallResult(IEnumerable<TestSuite> testSuites)
    {
        int testSuiteCount = testSuites.Count();
        int totalTestCount = testSuites.Sum(t => t.TestCount);

        if(testSuites.All(t => t.AllSuccessful))
        {
            Utilities.WriteToConsoleWithColor(ConsoleColor.Green, $"{testSuiteCount}/{testSuiteCount} test suites ok\n{totalTestCount}/{totalTestCount} tests ok");
        }
        else
        {
            IEnumerable<TestSuite> failed = testSuites.Where(t => !t.AllSuccessful);
            int failedTestCount = failed.Sum(f => f.FailedTestCount);

            Utilities.WriteToConsoleWithColor(ConsoleColor.Red, () =>
            {
                Console.WriteLine($"{failed.Count()}/{testSuiteCount} test suites failed\n{failedTestCount}/{totalTestCount} tests failed\n");

                foreach(TestSuite f in failed)
                {
                    Console.WriteLine($"{f.Name}");

                    foreach(string failedTestName in f.FailedTestNames)
                    {
                        Console.WriteLine($"\t - {failedTestName}");
                    }
                }
            });
        }

        Console.WriteLine();
    }

    private static IEnumerable<TestSuite> CreateTests(string testFolder)
    {
        if(!Directory.Exists(testFolder))
        {
            throw new ArgumentException($"Directory {testFolder} does not exists!", nameof(testFolder));
        }

        string[] subDirectories = Directory.GetDirectories(testFolder);

        return subDirectories.Select(dir => new TestSuite(dir));
    }
}