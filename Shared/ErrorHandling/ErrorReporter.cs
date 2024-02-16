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

    public static void Report(this Error error) => Console.Error.WriteLine(error.GenerateReportString());

}