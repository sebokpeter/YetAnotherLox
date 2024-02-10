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
    public virtual IEnumerable<string> Errors => _errors.AsEnumerable();


    internal readonly static string _interpreterPath = "Lox/bin/Debug/net8.0/cslox"; // There is only one interpreter, so it can be static.

    internal readonly List<string> _results = [];
    internal readonly List<string> _errors = [];

    internal readonly IEnumerable<string> _expectedResults;

    internal const int TimeoutMS = 10_000; // How long can an individual script/line run for.
    internal readonly CancellationTokenSource _cts = new(TimeoutMS);


    [GeneratedRegex("Expect( runtime error)?: (?<expected>.*)$")]
    internal static partial Regex ExpectedOutputRegex();

    public abstract Task Run();

    public Test(string scriptPath)
    {
        if(!scriptPath.EndsWith(".lox"))
        {
            throw new ArgumentException("Path does not point to a Lox script.", nameof(scriptPath));
        }

        Name = Path.GetFileNameWithoutExtension(scriptPath);

        _expectedResults = File.ReadAllLines(scriptPath).Where(line => ExpectedOutputRegex().IsMatch(line))
                            .Select(line => ExpectedOutputRegex().Match(line).Groups["expected"].Value)
                            .Select(expected => expected.Trim());

    }

    internal virtual void CheckErrors(IEnumerable<string> results)
    {
        if(results.Count() != _expectedResults.Count())
        {
            _errors.Add($"Expected {_expectedResults.Count()} results but got {results.Count()}.");
            return;
        }

        int i = 1;
        foreach(var (result, expected) in results.Zip(_expectedResults))
        {
            if(result != expected)
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
            if(!String.IsNullOrWhiteSpace(e.Data))
            {
                _results.Add(e.Data.Trim());
            }
        });

        lox.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
        {
            if(!String.IsNullOrWhiteSpace(e.Data))
            {
                _results.Add(e.Data.Trim());
            }
        });

        lox.StartInfo.FileName = _interpreterPath;

        return lox;
    }
}