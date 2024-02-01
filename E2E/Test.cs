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
public sealed partial class Test
{   
    public string Name => _scriptName;
    public bool Success => _errors.Count == 0;
    public IEnumerable<string> Errors => _errors.AsEnumerable(); // Just use strings for now. TODO: Create an Error object for better reporting

    private const int TimeoutMS = 5000; // 5 seconds

    private readonly List<string> _errors = [];
    private readonly List<string> _results = []; 
    private readonly static string _interpreterPath = "Lox/bin/Debug/net8.0/cslox"; // There is only one interpreter, so it can be static.
    private readonly string _testScriptPath;
    private readonly string _scriptName;
    private readonly IEnumerable<string> _expectedResults;     // The sequence of strings that the test script should print to the console.

    [GeneratedRegex("Expect( runtime error)?: (?<expected>.*)$")]
    private static partial Regex ExpectedOutputRegex();

    /// <summary>
    /// Construct a new Test object.
    /// This object is responsible for parsing the test script for the expected results, running the script, and verifying that the output of the script matches the expected results.
    /// 
    /// To indicate an expected value in the test script, use the following syntax: // Expect: expected_value
    /// Any number of expected results can be indicated.
    /// 
    /// To indicate an expected runtime error in the test script, use the following syntax: // Expect runtime error: runtime_error_message
    /// Since a runtime error terminates the script, only one runtime error should be expected.
    /// </summary>
    /// <param name="testScriptPath">The path of the .lox script that will be run by this <see cref="Test"/>.</param>
    public Test(string testScriptPath)
    {
        if (!testScriptPath.EndsWith(".lox"))
        {
            throw new ArgumentException("Path does not point to a Lox script.", nameof(testScriptPath));
        }

        _testScriptPath = testScriptPath;
        _scriptName = Path.GetFileNameWithoutExtension(testScriptPath);

        string[] sourceLines = File.ReadAllLines(testScriptPath);
        
        _expectedResults = sourceLines.Where(line => ExpectedOutputRegex().IsMatch(line))
                                      .Select(line => ExpectedOutputRegex().Match(line).Groups["expected"].Value);
    }

    public void Run()
    {
        Process lox = GetLoxProcess();

        lox.Start();
        lox.BeginOutputReadLine();
        lox.BeginErrorReadLine();

        bool exited = lox.WaitForExit(TimeoutMS); // Arbitrarily wait 5 seconds for the script to run 

        if(!exited)
        {
            _errors.Add($"Script did not finish in {TimeoutMS} milliseconds.");
            return;
        }

        CheckErrors(_results);
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
        lox.StartInfo.RedirectStandardError = true;
        lox.StartInfo.UseShellExecute = false;

        // Use OutputDataReceived and ErrorDataReceived to save data written to the standard and error output streams. 
        // This means that the list '_results' will contain the output of the process in order, making the comparison with the expected values easy.

        lox.OutputDataReceived += new DataReceivedEventHandler((sender, e) => {
            if(!String.IsNullOrWhiteSpace(e.Data))
            {
                _results.Add(e.Data);
            }
        });

        lox.ErrorDataReceived += new DataReceivedEventHandler((sender, e) => {
            if(!String.IsNullOrWhiteSpace(e.Data))
            {
                _results.Add(e.Data);
            }
        });

        lox.StartInfo.FileName = _interpreterPath;
        lox.StartInfo.ArgumentList.Add(_testScriptPath);

        return lox;
    }
}