using System.Diagnostics;

namespace E2E;

public class Program
{
    public static void Main()
    {
        IEnumerable<TestSuite> tests = CreateTests("E2E/scripts");

        foreach(TestSuite test in tests)
        {
            test.Run();
        }
    }

    private static IEnumerable<TestSuite> CreateTests(string testFolder)
    {
        if(!Directory.Exists(testFolder))
        {
            throw new ArgumentException($"Directory {testFolder} does not exists!", nameof(testFolder));
        }

        string[] subDirectories = Directory.GetDirectories(testFolder);
    
        return subDirectories.Select(dir => new TestSuite(dir));
    }
}