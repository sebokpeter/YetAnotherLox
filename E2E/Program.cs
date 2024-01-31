namespace E2E;

public class Program
{
    public static void Main()
    {
        ConsoleColor defaultColor = Console.ForegroundColor;

        Test t = new("E2E/scripts/addition.lox");

        t.Run();

        if(!t.Success) 
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("Failure");
            foreach (string error in t.Errors)
            {
                Console.WriteLine($" {error}");
            }
            Console.ForegroundColor = defaultColor;
        } 
        else 
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Success");
            Console.ForegroundColor = defaultColor;
        };
    }
}