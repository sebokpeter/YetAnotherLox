namespace E2E;

public class Program
{
    public static void Main()
    {
        TestSuite testSuite = new("E2E/scripts/math");
        testSuite.Run();
    }
}