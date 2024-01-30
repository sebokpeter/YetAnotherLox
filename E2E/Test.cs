using System.Diagnostics;

namespace E2E;

// The Test class should read in a .lox file, extract the necessary information (expected output, errors, etc), and then run them using the debug version of the interpreter.
// Redirect the console output to read the results. 

/// <summary>
/// Represents a single test case.
/// </summary>
public sealed class Test // TODO: create test case from .lox files
{   
    private readonly string _interpreterPath;

    public Test()
    {
        _interpreterPath = "Lox/bin/Debug/net8.0/cslox";
    }

    public void Run()
    {
        Process lox = GetLoxProcess();

        lox.Start();
        lox.WaitForExit();
    }

    private Process GetLoxProcess()
    {
        Process lox = new();
        lox.StartInfo.FileName = this._interpreterPath;

        return lox;
    }
}