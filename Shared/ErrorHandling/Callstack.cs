namespace Shared.ErrorHandling;

public record StackTrace(IEnumerable<Frame> Frames);

public record Frame(int Line, string FunctionName);
