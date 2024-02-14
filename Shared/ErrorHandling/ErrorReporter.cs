namespace Shared.ErrorHandling;

/// <summary>
/// A simple error reporter. 
/// Reports errors by writing to <see cref="Console.Error"/>.
/// </summary>
public static class ErrorReporter
{

    /// <summary>
    /// Report all errors in this <see cref="IEnumerable{Error}"/>.
    /// </summary>
    /// <param name="errors">The error to be reported.</param>
    public static void ReportAll(this IEnumerable<Error> errors) 
    {
        foreach (Error error in errors)
        {
            error.Report();
        }
    }

    public static void Report(this Error error)
    {
        if(error is ScanError scanError)
        {
            Report(scanError);
        }
        else if(error is ParseError parseError)
        {
            Report(parseError);
        }
        else if(error is ResolveError resolveError)
        {
            Report(resolveError);
        }
        else if(error is RuntimeError runtimeError)
        {
            Report(runtimeError);
        }
    }
    public static void Report(this ScanError error) => Console.Error.WriteLine($"[line {error.Line}] Scan Error: {error.Message}");
    public static void Report(this ParseError error)
    {
        if(error.Location is null)
        {
            Console.Error.WriteLine($"{error.Message}");
        }
        else 
        {
            Console.Error.WriteLine($"[line {error.Location.Line}] Parse Error {GetLocationString(error.Location)}: {error.Message}");
        }
    }

    public static void Report(this ResolveError error)
    {
        if(error.Location is null)
        {
            Console.Error.WriteLine(error.Message);
        }
        else
        {
            Console.Error.WriteLine($"[line {error.Location.Line}] Resolution Error {GetLocationString(error.Location)}: {error.Message}");
        }
    }

    public static void Report(this RuntimeError error)
    {
        if(error.Line is null && error.Token is null)
        {
            Console.Error.WriteLine(error.Message);
        }
        else if(error.Token is null)
        {
            Console.Error.WriteLine($"[line {error.Line}] Runtime Error: {error.Message}");
        }
        else 
        {
            Console.Error.WriteLine($"[line {error.Token.Line}] Runtime Error {GetLocationString(error.Token)}: {error.Message}");
        }
    }

    private static string GetLocationString(Token token)
    {
        return token.Type switch
        {
            TokenType.EOF => "at end",
            _ => $"at '{token.Lexeme}'"
        };
    }

    // TODO: other errors
}