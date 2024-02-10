namespace E2E;

public static class Utilities
{
    /// <summary>
    /// Set the console color to <paramref name="color"/>, invoke <paramref name="writeAction"/>, and reset the console color to the default <see cref="ConsoleColor"/>.
    /// <paramref name="writeAction"/> can be used to perform more complex tasks.
    /// </summary>
    /// <param name="color">The color of the output text.</param>
    /// <param name="writeAction">An <see cref="Action"/> that will write to the console.</param>
    public static void WriteToConsoleWithColor(ConsoleColor color, Action writeAction)
    {
        ConsoleColor defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        writeAction.Invoke();
        Console.ForegroundColor = defaultColor;
    }

    /// <summary>
    /// Set the console color to <paramref name="color"/>, write <paramref name="text"/>, and reset the console color to the default <see cref="ConsoleColor"/>.
    /// </summary>
    /// <param name="color">The color of the output text.</param>
    /// <param name="text">The output text</param>
    public static void WriteToConsoleWithColor(ConsoleColor color, string text)
    {
        ConsoleColor defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = defaultColor;
    }
}