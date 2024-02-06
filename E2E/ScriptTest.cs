using System.Diagnostics;
using System.Text.RegularExpressions;

namespace E2E;

// The Test class should read in a .lox file, extract the necessary information (expected output, errors, etc), and then run them using the debug version of the interpreter.
// Redirect the console output to read the results. 

/// <summary>
/// Represents a .lox file, used to verify the interpreter. 
/// The <see cref="ScriptTest"/> class is responsible for the following:
/// - Parsing the .lox file, to extract the expected results.
/// - Running the .lox file, using the debug version of the interpreter.
/// - Retrieving the results of the run.
/// - Ensuring that the results match the expected values.
/// </summary>
sealed class ScriptTest : Test
{
    private readonly string _testScriptPath;

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
    /// <param name="testScriptPath">The path of the .lox script that will be run by this <see cref="ScriptTest"/>.</param>
    public ScriptTest(string testScriptPath) : base(testScriptPath)
    {
        _testScriptPath = testScriptPath;
    }

    public override async Task Run()
    {
        Process lox = GetLoxProcess();

        lox.Start();
        lox.BeginOutputReadLine();
        lox.BeginErrorReadLine();

        try
        {
            await lox.WaitForExitAsync(_cts.Token);
        }
        catch(OperationCanceledException)
        {
            _errors.Add($"Script ({Name}) did not finish in {TimeoutMS} milliseconds");
            return;
        }

        CheckErrors();
    }

    internal override Process GetLoxProcess()
    {
        Process lox = base.GetLoxProcess();

        lox.StartInfo.ArgumentList.Add(_testScriptPath);

        return lox;
    }

}