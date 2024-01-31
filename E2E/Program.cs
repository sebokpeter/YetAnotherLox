namespace E2E;

public class Program
{
    public static void Main()
    {
        ConsoleColor defaultColor = Console.ForegroundColor;

        Test t = new("E2E/scripts/addition.lox");

        if(!t.Run()) 
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("Failure");
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