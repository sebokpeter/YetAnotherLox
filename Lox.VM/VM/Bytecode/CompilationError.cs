using Shared;

namespace LoxVM.VM;

internal record CompilationError(Token Token, string Message);