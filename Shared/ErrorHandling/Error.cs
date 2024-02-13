namespace Shared.ErrorHandling;

/// <summary>
/// Base type for compile-time and runtime errors.
/// </summary>
internal record Error(string Message);

/// <summary>
/// Type representing errors that occurred during compilation. 
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="Location">The <see cref="Token"/> where the error occurred.</param>
internal record CompileError(string Message, Token? Location) : Error(Message);

/// <summary>
/// Type representing errors that occurred while the program was running.
/// </summary>
/// /// <param name="Message">The error message.</param>
/// <param name="Line">The line number where the error occurred.</param>
internal record RuntimeError(string Message, int Line) : Error(Message);