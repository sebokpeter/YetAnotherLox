using System.Diagnostics;
using System.Text.RegularExpressions;

namespace E2E.Test;

abstract partial class Test
{
    /// <summary>
    /// The name of the test case. Usually the name of the test .lox script (e.g. if the script is 'addition.lox', the <see cref="Name"/> will be 'addition')
    /// </summary>
    public virtual string Name { get; protected set; }

    /// <summary>
    /// True if all outputs match the expected values, false otherwise.
    /// </summary>
    public virtual bool Success => _errors.Count == 0;

    /// <summary>
    /// An <see cref="IEnumerable{string}"/>, containing the error encountered. 
    /// Note that not it may not contain all errors, if, for example, one of the errors caused the test to return early (e.g. the script did not finish in <see cref="TimeoutMS"/> milliseconds);
    /// TODO: Create an 'Error' class to better represent errors
    /// </summary>
    public virtual IEnumerable<string> Errors => _errors;

    /// <summary>
    /// The path of the VM or Interpreter that will execute this test.
    /// </summary>
    internal string ExecutorPath { get; init; }
    internal readonly List<string> _results = [];
    internal readonly List<string> _errors = [];

    internal readonly IEnumerable<string> _expectedResults;

    internal const int TimeoutMS = 10_000; // How long can an individual script/line run for.
    internal readonly CancellationTokenSource _cts = new(TimeoutMS);


    /// <summary>
    /// Generate a regex that is used to parse the test inputs.
    /// The regex removes information such as the line number and the stage where the error happened.
    /// For example, the error "[line 1] Scan Error: Unterminated multiline comment." will become "Unterminated multiline comment."
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"Expect: ((\[line [0-9]*\])? (((Scan|Parse|Resolve|Runtime|Compiler) )?Error)?( at (end|\'.*\'))?:)?(?<expected>.*)$")]
    internal static partial Regex ExpectedOutputRegex();

    /// <summary>
    /// Generate a regex that is used to parse the test results.
    /// Behaves the same as <see cref="Test.ExpectedOutputRegex"/>, except does not not expect the "Expect: " prefix.
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"((\[line [0-9]*\])? (((Scan|Parse|Resolve|Runtime|Compiler) )?Error( at (end|\'.*\'))?: ))?(?<expected>.*)$")]
    internal static partial Regex OutputRegex();

    public abstract Task Run();

    public Test(string scriptPath, string executorPath)
    {
        if (!scriptPath.EndsWith(".lox"))
        {
            throw new ArgumentException("Path does not point to a Lox script.", nameof(scriptPath));
        }

        if (!File.Exists(executorPath))
        {
            throw new ArgumentException($"VM or Interpreter at {executorPath} does not exists.", nameof(executorPath));
        }

        Name = Path.GetFileNameWithoutExtension(scriptPath);

        ExecutorPath = executorPath;

        _expectedResults = File.ReadAllLines(scriptPath).Where(line => ExpectedOutputRegex().IsMatch(line))
                            .Select(line => ExpectedOutputRegex().Match(line).Groups["expected"].Value)
                            .Select(expected => expected.Trim());

    }

    /// <summary>
    /// Check the results. There are two checks:
    /// 1. The number of results. If the number of actual results does not match the number of expected results, an error is reported.
    /// 2. Check each actual result against the expected result. If they do not match, report an error.
    /// If <paramref name="checkResultsVerbatim"/> is true, the actual results is parsed by the generated <see cref="Test.ExpectedOutputRegex()"/> Regex, to extract the message.
    /// This strips away information such as the line number, and in which stage did the error occurred.
    /// For example, the error "[line 1] Scan Error: Unterminated multiline comment." will be interpreted simply as "Unterminated multiline comment."
    /// </summary>
    /// <param name="results">The <see cref="IEnumerable{String}"/> that contains the results.</param>
    /// <param name="checkResultsVerbatim">A switch that indicates whether of not the results should be interpreted verbatim.</param>
    internal virtual void CheckErrors(IEnumerable<string> results, bool checkResultsVerbatim = false)
    {
        if (results.Count() != _expectedResults.Count())
        {
            _errors.Add($"Expected {_expectedResults.Count()} results but got {results.Count()}.");
            return;
        }

        if (!checkResultsVerbatim)
        {
            results = results.Select(res => OutputRegex().Match(res).Groups["expected"].Value.Trim());
        }

        int i = 1;
        foreach (var (result, expected) in results.Zip(_expectedResults))
        {
            if (result != expected)
            {
                _errors.Add($"Expected '{expected}' but got '{result}' (position {i}).");
            }
            i++;
        }
    }

    internal virtual void CheckErrors() => CheckErrors(_results);

    internal virtual Process GetLoxProcess()
    {
        Process lox = new();

        lox.StartInfo.RedirectStandardOutput = true;
        lox.StartInfo.RedirectStandardError = true;
        lox.StartInfo.RedirectStandardInput = true;
        lox.StartInfo.UseShellExecute = false;

        lox.EnableRaisingEvents = true;

        // Use OutputDataReceived and ErrorDataReceived to save data written to the standard and error output streams. 
        // This means that the list '_results' will contain the output of the process in order, making the comparison with the expected values easy.
        // Note, the string is trimmed before it is added to '_results', so whitespaces are disregarded. 

        lox.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
        {
            if (!String.IsNullOrWhiteSpace(e.Data))
            {
                _results.Add(e.Data.Trim());
            }
        });

        lox.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
        {
            if (!String.IsNullOrWhiteSpace(e.Data))
            {
                _results.Add(e.Data.Trim());
            }
        });

        lox.StartInfo.FileName = ExecutorPath;

        return lox;
    }
}