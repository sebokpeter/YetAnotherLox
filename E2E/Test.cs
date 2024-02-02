using System.Diagnostics;
using System.Text.RegularExpressions;

namespace E2E;

abstract partial class Test
{
    public virtual string Name { get; private set; }
    public virtual bool Success => _errors.Count == 0;
    public virtual IEnumerable<string> Errors => _errors.AsEnumerable(); // Just use strings for now. TODO: Create an Error object for better reporting


    internal const int TimeoutMS = 5000;

    internal readonly static string _interpreterPath = "Lox/bin/Debug/net8.0/cslox"; // There is only one interpreter, so it can be static.

    internal readonly List<string> _results = [];
    internal readonly List<string> _errors = [];

    internal readonly IEnumerable<string> _expectedResults;


    [GeneratedRegex("Expect( runtime error)?: (?<expected>.*)$")]
    internal static partial Regex ExpectedOutputRegex();

    public abstract void Run();

    public Test(string scriptPath)
    {
        if(!scriptPath.EndsWith(".lox"))
        {
            throw new ArgumentException("Path does not point to a Lox script.", nameof(scriptPath));
        }

        Name = Path.GetFileNameWithoutExtension(scriptPath);

        _expectedResults = File.ReadAllLines(scriptPath).Where(line => ExpectedOutputRegex().IsMatch(line))
                            .Select(line => ExpectedOutputRegex().Match(line).Groups["expected"].Value);

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

        lox.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
        {
            if(!String.IsNullOrWhiteSpace(e.Data))
            {
                _results.Add(e.Data);
            }
        });

        lox.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
        {
            if(!String.IsNullOrWhiteSpace(e.Data))
            {
                _results.Add(e.Data);
            }
        });

        lox.StartInfo.FileName = _interpreterPath;

        return lox;
    }
}