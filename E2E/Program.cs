using System.Diagnostics;

namespace E2E;

public class Program
{
    public static void Main()
    {
        TestSuite math = new("E2E/scripts/math");
        math.Run();

        TestSuite str = new("E2E/scripts/string");
        str.Run();
    }
}