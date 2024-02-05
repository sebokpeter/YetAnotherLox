using System.Diagnostics;

namespace E2E;

public class Program
{
    public static void Main()
    {
        IEnumerable<TestSuite> tests = CreateTests("E2E/scripts");

        RunTest(tests);
    }

    private static void RunTest(IEnumerable<TestSuite> tests)
    {
        TestSuite[] testSuites = tests.ToArray(); // Materialize the collection, so that when test suites run, their updated status is saved

        Stopwatch sw = Stopwatch.StartNew();
        foreach(TestSuite test in testSuites)
        {
            test.Run();
        }
        sw.Stop(); // Not great, since also includes time spent Console IO
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
            IEnumerable<TestSuite> failed = tests.Where(t => !t.AllSuccessful);

            Utilities.WriteToConsoleWithColor(ConsoleColor.Red, () => {
                Console.WriteLine($"{failed.Count()}/{testCount} failed: \n");

                foreach (TestSuite f in failed)
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