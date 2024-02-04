
using System.Diagnostics;

namespace E2E;

/// <summary>
/// Represent multiple, independent test cases in a .lox file.
/// The <see cref="LineTest"/> class is responsible for the following:
/// - Parsing the .lox file, to extract each line and the expected result.
/// - Run each line independently.
/// - Verify that each line produces the expected result.
/// </summary>
sealed class LineTest : Test
{
    private readonly IEnumerable<string> _lines;

    public LineTest(string testScriptPath) : base(testScriptPath)
    {
        _lines = File.ReadAllLines(testScriptPath);
    }

    public override void Run()
    {
        Process lox = GetLoxProcess();
        lox.Start();
        lox.BeginOutputReadLine();
        lox.BeginErrorReadLine();

        using(StreamWriter inputWriter = lox.StandardInput)
        {
            foreach(string line in _lines)
            {
                inputWriter.WriteLine(line);
            }
        }

        bool exited = lox.WaitForExit(TimeoutMS);

        if(!exited)
        {
            _errors.Add($"Script ({Name}) did not finish in {TimeoutMS} milliseconds");
            return;
        }

        IEnumerable<string> trimmedResults = _results.Select(res => res.Replace(">", "").Trim()).Where(res => !String.IsNullOrWhiteSpace(res));

        CheckErrors(trimmedResults);
    }
}