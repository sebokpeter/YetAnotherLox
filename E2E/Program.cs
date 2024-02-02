using System.Diagnostics;

namespace E2E;

public class Program
{
    public static void Main()
    {
        // TestSuite testSuite = new("E2E/scripts/math");
        // testSuite.Run();

        LineTest lineTest = new("E2E/scripts/math/addition.lox");
        lineTest.Run();

        Debugger.Break();
    }
}