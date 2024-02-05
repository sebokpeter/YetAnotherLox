using System.Diagnostics;

namespace E2E;

/// <summary>
/// Represents a series of related tests.
/// </summary>
public class TestSuite
{

    public string AAA = "";

    /// <summary>
    /// <para/>Reports if all the <see cref="Test"/>s in this <see cref="TestSuite"/> have been successfully executed. 
    /// <para/>Reports false if either: 
    /// <para/>1. The tests have not yet been run, or
    /// <para/>2. At least one of the tests failed.
    /// </summary>
    public bool AllSuccessful => _alreadyRun && _tests.All(t => t.Success);

    /// <summary>
    /// The name of the test suite, parsed from the folder name that contains the test scripts.
    /// </summary>
    public string Name => _testSuitName;

    private readonly string _testSuitName;
    private readonly IEnumerable<Test> _tests;

    private const ConsoleColor SuccessColor = ConsoleColor.Green;
    private const ConsoleColor FailureColor = ConsoleColor.Red;

    private bool _alreadyRun = false;

    public TestSuite(string folder)
    {
        if(!Directory.Exists(folder))
        {
            throw new ArgumentException($"{folder} is not an existing folder.", nameof(folder));
        }

        _testSuitName = Path.GetFileName(folder).TrimEnd(Path.DirectorySeparatorChar);

        _tests = Directory.GetFiles(folder).Where(file => file.EndsWith(".lox")).Select<string, Test>(file =>
        {
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
        _alreadyRun = true;
    }

    private static void ReportTestResult(Test test)
    {
        Console.Write($"\t{test.Name} - ");

        if(test.Success)
        {
            Utilities.WriteToConsoleWithColor(SuccessColor, "Ok");
        }
        else
        {
            Utilities.WriteToConsoleWithColor(FailureColor, () =>
            {
                Console.WriteLine("Failed");

                foreach(string err in test.Errors)
                {
                    Console.WriteLine($"\t\t{err}");
                }
            });
        }
    }
}