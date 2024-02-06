using System.Collections.Concurrent;
using System.Diagnostics;

namespace E2E;

public class Program
{
    public static async Task Main()
    {
        IEnumerable<TestSuite> tests = CreateTests("E2E/scripts");

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

    private static void DisplayOverallResult(IEnumerable<TestSuite> tests)
    {
        int testCount = tests.Count();

        if(tests.All(t => t.AllSuccessful))
        {
            Utilities.WriteToConsoleWithColor(ConsoleColor.Green, $"{testCount}/{testCount} ok");
        }
        else
        {
            // TODO: report individual failed tests, instead of test suites.
            IEnumerable<TestSuite> failed = tests.Where(t => !t.AllSuccessful);

            Utilities.WriteToConsoleWithColor(ConsoleColor.Red, () =>
            {
                Console.WriteLine($"{failed.Count()}/{testCount} failed: \n");

                foreach(TestSuite f in failed)
                {
                    Console.WriteLine($"{f.Name}");
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