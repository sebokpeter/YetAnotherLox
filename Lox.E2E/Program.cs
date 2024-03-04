using System.Collections.Concurrent;
using System.Diagnostics;

using E2E.Test;

namespace E2E;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length != 1 || !(args[0] == "-v" || args[0] == "-i"))
        {
            Console.WriteLine("Usage: ");
            Console.WriteLine("loxe2e (-v | -i)");
            Console.WriteLine("Arguments: ");
            Console.WriteLine("\t-v: Test the Lox virtual machine.");
            Console.WriteLine("\t-i: Test the Lox interpreter.");
        }
        else
        {
            string targetPath = args[0] == "-v" ? "Lox.VM/bin/Debug/net8.0/vmlox" : "Lox.Interpreted/bin/Debug/net8.0/cslox";

            IEnumerable<TestSuite> tests = CreateTests("Lox.E2E/scripts", targetPath);

            await RunTest(tests);
        }
    }

    private static async Task RunTest(IEnumerable<TestSuite> tests)
    {
        ConcurrentBag<TestSuite> testSuites = new(tests);

        Stopwatch sw = Stopwatch.StartNew();
        await Parallel.ForEachAsync(testSuites, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (testSuite, token) => await testSuite.Run());
        sw.Stop();

        foreach (TestSuite test in testSuites)
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

        if (testSuites.All(t => t.AllSuccessful))
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

                foreach (TestSuite f in failed)
                {
                    Console.WriteLine($"{f.Name}");

                    foreach (string failedTestName in f.FailedTestNames)
                    {
                        Console.WriteLine($"\t - {failedTestName}");
                    }
                }
            });
        }

        Console.WriteLine();
    }

    private static IEnumerable<TestSuite> CreateTests(string testFolder, string targetPath)
    {
        if (!Directory.Exists(testFolder))
        {
            throw new ArgumentException($"Directory {testFolder} does not exists!", nameof(testFolder));
        }

        if (!File.Exists(targetPath))
        {
            throw new ArgumentException($"VM or Interpreter could not be found at {targetPath}.", nameof(targetPath));
        }

        string[] subDirectories = Directory.GetDirectories(testFolder);

        return subDirectories.Select(dir => new TestSuite(dir, targetPath));
    }
}