namespace LoxConsole.Interpreter;

/// <summary>
/// Represents a callable object, such as a function, method, or class.
/// </summary>
internal interface ILoxCallable 
{
    /// <summary>
    /// The number of arguments this <see cref="ILoxCallable"> takes.
    /// </summary>
    int Arity {get; }

    /// <summary>
    /// Call this <see cref="ILoxCallable"/> using the provided interpreter, and list of arguments.
    /// </summary>
    /// <param name="interpreter">An interpreter, used to execute this callable.</param>
    /// <param name="arguments">The arguments provided to this callable.</param>
    /// <returns>The result of the call, or <see langword="null"/> if the call produces no value.</returns>
    object Call(Interpreter interpreter, List<object> arguments);
}