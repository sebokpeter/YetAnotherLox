using System.Diagnostics;

namespace E2E;

/// <summary>
/// Represents a series of related tests.
/// </summary>
public class TestSuite
{
    private readonly string _testSuitName;
    private readonly List<ScriptTest> _tests;

    private readonly ConsoleColor _defaultForegroundColor;

    private const ConsoleColor SuccessColor = ConsoleColor.Green;
    private const ConsoleColor FailureColor = ConsoleColor.Red;

    public TestSuite(string folder)
    {
        if(!Directory.Exists(folder))
        {
            throw new ArgumentException($"{folder} is not an existing folder.", nameof(folder));
        }

        _testSuitName = Path.GetFileName(folder).TrimEnd(Path.DirectorySeparatorChar);

        _tests = Directory.GetFiles(folder).Where(file => file.EndsWith(".lox")).Select(file => new ScriptTest(file)).ToList();

        _defaultForegroundColor = Console.ForegroundColor;
    }

    public void Run()
    {
        Console.WriteLine($"Running tests for '{_testSuitName}':");
        foreach(ScriptTest test in _tests)
        {
            test.Run();
            ReportTestResult(test);
        }
        Console.WriteLine("-----------------------------");
    }

    private void ReportTestResult(ScriptTest test)
    {
        Console.Write($"\t{test.Name} - ");

        if(test.Success)
        {
            WriteToConsoleWithColor(SuccessColor, () => Console.WriteLine("Ok"));
        }
        else 
        {
            WriteToConsoleWithColor(FailureColor, () => {
                Console.WriteLine("Failed");

                foreach(string err in test.Errors)
                {
                    Console.WriteLine($"\t\t{err}");
                }
            });
        }
    }

    private void WriteToConsoleWithColor(ConsoleColor color, Action writeAction)
    {
        Console.ForegroundColor = color;
        writeAction();
        Console.ForegroundColor = _defaultForegroundColor;
    }
}