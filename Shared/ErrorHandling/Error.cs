namespace Shared.ErrorHandling;

/// <summary>
/// Base type for compile-time and runtime errors.
/// </summary>
public abstract record Error(string Message)
{
    /// <summary>
    /// Generate a string that describes this <see cref="Error"/>.
    /// </summary>
    /// <returns></returns>
    internal virtual string GenerateReportString() => $"Unknown Error: {Message}";
}

/// <summary>
/// Type representing errors that occurred during scanning.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="Line">The line number where the error occurred.</param>
public record ScanError(string Message, int Line) : Error(Message)
{
    /// <summary>
    /// Generate a string that describes this <see cref="ScanError"/>, including the line in the source where the error occurred.
    /// </summary>
    /// <returns></returns>
    internal override string GenerateReportString() => $"[line {Line}] Scan Error: {Message}";
}

/// <summary>
/// Type representing errors that occurred during parsing.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="Location">The <see cref="Token"/> where the error occurred.</param>
public record ParseError(string Message, Token? Location) : Error(Message)
{
    /// <summary>
    /// Generate a string that describes this <see cref="ParseError"/>. If <see cref="ParseError.Location"/> is not null, also include the line in the source where the error occurred.
    /// </summary>
    /// <returns></returns>
    internal override string GenerateReportString() => Location is null ? $"Scan Error: {Message}" : $"[line {Location.Line}] Parse Error {Location.GetLocationString()}: {Message}";
}

/// <summary>
/// Type representing errors that occurred during the variable resolution pass.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="Location">The <see cref="Token"/> where the error occurred.</param>
public record ResolveError(string Message, Token? Location) : Error(Message)
{
    /// <summary>
    /// Generate a string that describes this <see cref="ResolveError"/>. If <see cref="ResolveError.Location"/> is not null, also include the line in the source where the error occurred.
    /// </summary>
    /// <returns></returns>
    internal override string GenerateReportString() => Location is null ? $"Resolve Error: {Message}" : $"[line {Location.Line}] Resolve Error {Location.GetLocationString()}: {Message}";
}

/// <summary>
/// Type representing errors that occurred during the bytecode emission pass.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="Location">The <see cref="Token"/> where the error occurred.</param>
public record CompilerError(string Message, Token? Location) : Error(Message)
{
    /// <summary>
    /// Generate a string that describes this <see cref="CompilerError"/>. If <see cref="CompilerError.Location"/> is not null, also include the line in the source where the error occurred.
    /// </summary>
    /// <returns></returns>
    internal override string GenerateReportString() => Location is null ? $"Compiler Error: {Message}" : $"[line {Location.Line}] Compiler Error {Location.GetLocationString()}: {Message}";
}

/// <summary>
/// Type representing errors that occurred while the program was running.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="Line">The line number where the error occurred.</param>
/// <param name="Token">The <see cref="Token"/> where the error occurred.</param>
public record RuntimeError(string Message, int? Line, Token? Token, StackTrace? Trace = null) : Error(Message)
{
    /// <summary>
    /// Generate a string that describes this <see cref="RuntimeError"/>. If at least one of the members <see cref="RuntimeError.Line"/> or <see cref="RuntimeError.Token"/> is not null, also include the location of the error in the source.
    /// </summary>
    /// <returns></returns>
    internal override string GenerateReportString()
    {
        string message;

        if (Line is null && Token is null)
        {
            message = $"Runtime Error: {Message}";
        }
        else if (Token is null)
        {
            message = $"[line {Line}] Runtime Error: {Message}";
        }
        else
        {
            message = $"[line {Token.Line}] Runtime Error {Token.GetLocationString()}: {Message}";
        }

#if PRINT_STACKTRACE
        if (Trace is not null)
        {
            message += "\n";
            message += String.Join('\n', Trace.Frames.Select(frame => $"[line {frame.Line}] in {frame.FunctionName}."));
        }
#endif

        return message;
    }
}

internal static class TokenExtension
{
    internal static string GetLocationString(this Token token)
    {
        return token.Type switch
        {
            TokenType.EOF => "at end",
            _ => $"at '{token.Lexeme}'"
        };
    }
}