namespace Shared.ErrorHandling;

/// <summary>
/// Base type for compile-time and runtime errors.
/// </summary>
public record Error(string Message);

/// <summary>
/// Type representing errors that occurred during scanning.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="Line">The line number where the error occurred.</param>
public record ScanError(string Message, int Line) : Error(Message);

/// <summary>
/// Type representing errors that occurred during parsing. 
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="Location">The <see cref="Token"/> where the error occurred.</param>
public record ParseError(string Message, Token? Location) : Error(Message);

/// <summary>
/// Type representing errors that occurred while the program was running.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="Line">The line number where the error occurred.</param>
public record RuntimeError(string Message, int Line) : Error(Message);