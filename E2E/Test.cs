using System.Diagnostics;
using System.Text.RegularExpressions;

namespace E2E;

// The Test class should read in a .lox file, extract the necessary information (expected output, errors, etc), and then run them using the debug version of the interpreter.
// Redirect the console output to read the results. 

/// <summary>
/// Represents a single test case (a .lox file).
/// The <see cref="Test"/> class is responsible for the following:
/// - Parsing the .lox script, to extract the expected results.
/// - Running the .lox script, using the debug version of the interpreter.
/// - Retrieving the results of the run.
/// - Ensuring that the results match the expected values.
/// </summary>
public sealed partial class Test // TODO: error messages
{   
    public bool Success => _errors.Count == 0;
    public IEnumerable<string> Errors => _errors.AsEnumerable(); // Just use strings for now. TODO: Create an Error object for better reporting

    private const int TimeoutMS = 5000; // 5 seconds

    private readonly List<string> _errors = [];
    private readonly static string _interpreterPath = "Lox/bin/Debug/net8.0/cslox"; // There is only one interpreter, so it can be static.
    private readonly string _testScriptPath;
    private readonly IEnumerable<string> _expectedResults;     // The sequence of strings that the test script should print to the console.

    [GeneratedRegex("Expect: (?<expected>.*)$")]
    private static partial Regex ExpectedOutputRegex();

    /// <summary>
    /// Construct a new Test object.
    /// </summary>
    /// <param name="testScriptPath">The path of the .lox script that will be run by this <see cref="Test"/>.</param>
    public Test(string testScriptPath)
    {
        if (!testScriptPath.EndsWith(".lox"))
        {
            throw new ArgumentException("Path does not point to a Lox script.", nameof(testScriptPath));
        }

        _testScriptPath = testScriptPath;

        string[] sourceLines = File.ReadAllLines(testScriptPath);
        
        _expectedResults = sourceLines.Where(line => ExpectedOutputRegex().IsMatch(line))
                                      .Select(line => ExpectedOutputRegex().Match(line).Groups["expected"].Value);
    }

    public void Run()
    {
        Process lox = GetLoxProcess();

        lox.Start();
        bool exited = lox.WaitForExit(TimeoutMS); // Arbitrarily wait 5 seconds for the script to run 

        if(!exited)
        {
            _errors.Add($"Script did not finish in {TimeoutMS} milliseconds.");
            return;
        }

        IEnumerable<string> resultLines = lox.StandardOutput.ReadToEnd().Split('\n').Where(line => !String.IsNullOrWhiteSpace(line));
        
        CheckErrors(resultLines);
    }

    private void CheckErrors(IEnumerable<string> resultLines)
    {
        if(resultLines.Count() != _expectedResults.Count())
        {
            _errors.Add($"Expected {_expectedResults.Count()} results but got {resultLines.Count()}.");
            return;
        }

        int i = 1;
        foreach(var (result, expected) in resultLines.Zip(_expectedResults))
        {
            if(result != expected)
            {
                _errors.Add($"Expected '{expected}' but got '{result}' (position {i}).");
            }
            i++;
        }
    }

    private Process GetLoxProcess()
    {
        Process lox = new();

        lox.StartInfo.RedirectStandardOutput = true;
        lox.StartInfo.UseShellExecute = false;

        lox.StartInfo.FileName = _interpreterPath;
        lox.StartInfo.ArgumentList.Add(_testScriptPath);

        return lox;
    }
}