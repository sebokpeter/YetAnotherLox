using System.Diagnostics;

namespace E2E;

/// <summary>
/// Represents a series of related tests.
/// </summary>
public class TestSuite
{
    private readonly string _testSuitName;
    private readonly IEnumerable<Test> _tests;

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

        _tests = Directory.GetFiles(folder).Where(file => file.EndsWith(".lox")).Select<string, Test>(file => {
            string fileName = Path.GetFileNameWithoutExtension(file);
            if(fileName.EndsWith(".line")) // Test files with the naming scheme <scriptName>.line.lox are run line-by-line
            {
                return new LineTest(file);
            }
            else
            {
                return new ScriptTest(file);
            }
        });

        _defaultForegroundColor = Console.ForegroundColor;
    }

    public void Run()
    {
        Console.WriteLine($"Running tests for '{_testSuitName}':");
        foreach(Test test in _tests)
        {
            test.Run();
            ReportTestResult(test);
        }
        Console.WriteLine("-----------------------------");
    }

    private void ReportTestResult(Test test)
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