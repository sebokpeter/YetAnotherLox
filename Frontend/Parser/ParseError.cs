using Shared;

namespace Frontend.Parser;

public record ParseError(Token Token, string Message);