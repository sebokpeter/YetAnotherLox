using System.Diagnostics;
using Generated;
using LoxVM.Chunk;
using LoxVM.Value;
using Shared;
using Shared.ErrorHandling;

namespace LoxVM.Compiler;

internal class BytecodeCompiler : Stmt.IVoidVisitor, Expr.IVoidVisitor
{
    internal bool HadError => _errors.Count > 0;
    internal IEnumerable<CompilerError> Errors => _errors;
    private readonly List<CompilerError> _errors;

    private readonly List<Stmt> _statements;

    private readonly ObjFunction _function;
    private readonly FunctionType _functionType;

    private const ushort MAX_LOCAL_COUNT = byte.MaxValue + 1;
    internal readonly Local[] _locals;
    internal int scopeDepth;
    internal int localCount;

    private int latestLine; // Remember the last line number we saw, so that we can use it when we want to add an instruction, but do not know the line number. (E.g. when emitting a POP instruction in EndScope())

    public BytecodeCompiler(List<Stmt> stmts)
    {
        _statements = stmts;
        _errors = [];

        _function = ObjFunction.TopLevel();
        _locals = new Local[MAX_LOCAL_COUNT];
        _locals[localCount++] = new() { Depth = 0, Name = "" };

        _functionType = FunctionType.Script;
    }

    public BytecodeCompiler(BytecodeCompiler compiler, FunctionType type, int arity, string fnName)
    {
        _statements = [];
        _errors = compiler._errors;

        _functionType = type;
        localCount = 0;
        scopeDepth = 0;

        _locals = new Local[MAX_LOCAL_COUNT];
        _function = new() { Arity = arity, Name = fnName, Chunk = new() };
        _locals[localCount++] = new() { Depth = 0, Name = "" };
    }

    public ObjFunction Compile()
    {
        // TODO
        foreach(Stmt stmt in _statements)
        {
            EmitBytecode(stmt);
        }

        EmitByte(OpCode.Nil);
        EmitByte(OpCode.Return);
        return _function;
    }

    private ObjFunction CompileFunction(Stmt.Function function) // TODO: remove hack
    {
        BeginScope();
        foreach(Token param in function.Params)
        {
            AddLocal(param);
            MarkInitialized();
        }

        EmitBytecode(function.Body);
        return _function;
    }

    private void EmitBytecode(Stmt stmt)
    {
        stmt.Accept(this);
    }

    private void EmitBytecode(Expr expr)
    {
        expr.Accept(this);
    }

    private void EmitBytecode(IEnumerable<Stmt> stmts)
    {
        foreach(Stmt stmt in stmts)
        {
            EmitBytecode(stmt);
        }
    }

    private void EmitBytecode(IEnumerable<Expr> exprs)
    {
        foreach(Expr expr in exprs)
        {
            EmitBytecode(expr);
        }
    }
    #region Statements

    public void VisitExpressionStmt(Stmt.Expression stmt)
    {
        EmitBytecode(stmt.Ex);
        EmitByte(OpCode.Pop, stmt.Line);
    }

    public void VisitReturnStmt(Stmt.Return stmt)
    {
        if(_functionType == FunctionType.Script)
        {
            AddError("Cannot return from top-level code.", stmt.Keyword);
        }

        if(stmt.Value is not null)
        {
            EmitBytecode(stmt.Value);
        }
        else 
        {
            EmitByte(OpCode.Nil, stmt.Keyword.Line);
        }

        EmitByte(OpCode.Return, stmt.Keyword.Line);
    }

    public void VisitPrintStmt(Stmt.Print stmt)
    {
        EmitBytecode(stmt.Ex);
        EmitByte(OpCode.Print, stmt.Line);
    }

    public void VisitVarStmt(Stmt.Var stmt)
    {
        DeclareVariable(stmt.Name);

        if(stmt.Initializer is not null)
        {
            EmitBytecode(stmt.Initializer);
        }
        else
        {
            EmitByte(OpCode.Nil, stmt.Name.Line);
        }

        DefineVariable(stmt.Name);
    }

    public void VisitBlockStmt(Stmt.Block stmt)
    {
        BeginScope();
        EmitBytecode(stmt.Statements);
        EndScope();
    }

    public void VisitIfStmt(Stmt.If stmt)
    {
        EmitBytecode(stmt.Condition);

        int thenJump = EmitJump(OpCode.JumpIfFalse, GetLineNumber(stmt.Condition));

        EmitByte(OpCode.Pop);
        EmitBytecode(stmt.ThenBranch);

        int elseJump = EmitJump(OpCode.Jump, GetLineNumber(stmt.Condition));

        PatchJump(thenJump);

        EmitByte(OpCode.Pop);
        if(stmt.ElseBranch is not null)
        {
            EmitBytecode(stmt.ElseBranch);
        }
        PatchJump(elseJump);
    }

    public void VisitWhileStmt(Stmt.While stmt)
    {
        int loopStart = _function.Chunk.Count;
        EmitBytecode(stmt.Condition);

        int line = GetLineNumber(stmt.Condition);
        int exitJump = EmitJump(OpCode.JumpIfFalse, line);
        EmitByte(OpCode.Pop, line);

        EmitBytecode(stmt.Body);
        EmitLoop(loopStart, line);

        PatchJump(exitJump);
        EmitByte(OpCode.Pop);
    }

    public void VisitForStmt(Stmt.For stmt)
    {
        BeginScope();
        if(stmt.Initializer is not null)
        {
            EmitBytecode(stmt.Initializer);
        }

        int loopStart = _function.Chunk.Count;

        (int exitJump, int conditionLine) = (-1, -1);
        if(stmt.Condition is not null)
        {
            EmitBytecode(stmt.Condition);

            conditionLine = GetLineNumber(stmt.Condition);

            exitJump = EmitJump(OpCode.JumpIfFalse, conditionLine);
            EmitByte(OpCode.Pop, conditionLine);
        }

        EmitBytecode(stmt.Body);

        if(stmt.Increment is not null)
        {
            EmitBytecode(stmt.Increment);
            EmitByte(OpCode.Pop, GetLineNumber(stmt.Increment));
        }

        EmitLoop(loopStart, stmt.Line);

        if(exitJump != -1)
        {
            PatchJump(exitJump);
            EmitByte(OpCode.Pop, conditionLine);
        }

        EndScope();
    }

    public void VisitFunctionStmt(Stmt.Function stmt)
    {
        // DeclareVariable(stmt.Name);
        // MarkInitialized();
        // Function(FunctionType.Function, stmt);
        // DefineVariable(stmt.Name);

        int global = ParseVariable(stmt.Name);
        MarkInitialized();
        Function(FunctionType.Function, stmt);
        DefineVariable(global);
    }

    private void DefineVariable(int global)
    {
        if(scopeDepth > 0)
        {
            MarkInitialized();
            return;
        }

        EmitBytes(OpCode.DefineGlobal, (byte)global, latestLine);
    }

    private int ParseVariable(Token name)
    {
        DeclareVariable(name);

        if(scopeDepth > 0)
        {
            return 0;
        }

        return IdentifierConstant(name);
    }

    private int IdentifierConstant(Token name) => MakeConstant(LoxValue.Object(name.Lexeme));

    private void Function(FunctionType type, Stmt.Function function)
    {
        int arity = function.Params.Count;
        string name = function.Name.Lexeme;

        BytecodeCompiler compiler = new(this, type, arity, name);

        ObjFunction fun = compiler.CompileFunction(function);

        EmitBytes(OpCode.Constant, MakeConstant(LoxValue.Object(fun)), latestLine);
    }


    #endregion

    #region Expressions

    public void VisitGroupingExpr(Expr.Grouping expr)
    {
        EmitBytecode(expr.Expression);
    }

    public void VisitBinaryExpr(Expr.Binary expr)
    {
        EmitBytecode(expr.Right);
        EmitBytecode(expr.Left);

        int line = expr.Operator.Line;

        switch(expr.Operator.Type)
        {
            case TokenType.PLUS:
                EmitByte(OpCode.Add, line);
                break;
            case TokenType.MINUS:
                EmitByte(OpCode.Subtract, line);
                break;
            case TokenType.STAR:
                EmitByte(OpCode.Multiply, line);
                break;
            case TokenType.SLASH:
                EmitByte(OpCode.Divide, line);
                break;
            case TokenType.MODULO:
                EmitByte(OpCode.Modulo, line);
                break;
            case TokenType.EQUAL_EQUAL:
                EmitByte(OpCode.Equal, line);
                break;
            case TokenType.BANG_EQUAL:
                EmitBytes(OpCode.Equal, OpCode.Not, line);
                break;
            case TokenType.LESS:
                EmitByte(OpCode.Less, line);
                break;
            case TokenType.LESS_EQUAL:
                EmitBytes(OpCode.Greater, OpCode.Not, line);
                break;
            case TokenType.GREATER:
                EmitByte(OpCode.Greater, line);
                break;
            case TokenType.GREATER_EQUAL:
                EmitBytes(OpCode.Less, OpCode.Not, line);
                break;
            default:
                throw new UnreachableException();
        }
    }

    public void VisitLogicalExpr(Expr.Logical expr)
    {
        EmitBytecode(expr.Right);
        EmitBytecode(expr.Left);

        int line = expr.Oper.Line;

        switch(expr.Oper.Type)
        {
            case TokenType.AND:
                EmitByte(OpCode.And, line);
                break;
            case TokenType.OR:
                EmitByte(OpCode.Or, line);
                break;
            default:
                throw new UnreachableException();
        }
    }

    public void VisitUnaryExpr(Expr.Unary expr)
    {
        EmitBytecode(expr.Right);
        switch(expr.Operator.Type)
        {
            case TokenType.BANG:
                EmitByte(OpCode.Not, expr.Operator.Line);
                break;
            case TokenType.MINUS:
                EmitByte(OpCode.Negate, expr.Operator.Line);
                break;
            default:
                throw new UnreachableException();
        }
    }

    public void VisitLiteralExpr(Expr.Literal expr)
    {
        int line = expr.Token is not null ? expr.Token.Line : -1; // TODO: Throw exception if Literal.Token is null? 

        switch(expr.Value)
        {
            case null:
                EmitByte(OpCode.Nil, line);
                break;
            case double d:
                EmitConstant(LoxValue.Number(d), line);
                break;
            case bool b:
                EmitByte(b ? OpCode.True : OpCode.False, line);
                break;
            case string s:
                EmitConstant(LoxValue.Object(s), line);
                break;
            default:
                throw new NotImplementedException($"{nameof(VisitLiteralExpr)} can not yet handle {expr.Value.GetType()} values.");
        }
    }

    public void VisitVariableExpr(Expr.Variable expr)
    {
        int arg = ResolveLocal(expr.Name);

        if(arg == -1)
        {
            // Did not find a local variable
            // Assume it is global
            ReadGlobal(expr);
        }
        else
        {
            EmitBytes(OpCode.GetLocal, (byte)arg, expr.Name.Line);
        }
    }

    public void VisitAssignExpr(Expr.Assign expr)
    {
        EmitBytecode(expr.Value);

        int arg = ResolveLocal(expr.Name);

        if(arg == -1)
        {
            AssignGlobal(expr);
        }
        else
        {
            EmitBytes(OpCode.SetLocal, (byte)arg, expr.Name.Line);
        }
    }

    public void VisitCallExpr(Expr.Call expr)
    {
        int count = expr.Arguments.Count;
        if(count > 255)
        {
            AddError("Can't have more than 255 parameters.", expr.Paren);
        }

        EmitBytecode(expr.Callee);

        EmitBytecode(expr.Arguments);
        EmitBytes(OpCode.Call, (byte)count, expr.Paren.Line);
    }

    #endregion

    public void VisitClassStmt(Stmt.Class stmt)
    {
        throw new NotImplementedException();
    }

    public void VisitBreakStmt(Stmt.Break stmt)
    {
        throw new NotImplementedException();
    }

    public void VisitContinueStmt(Stmt.Continue stmt)
    {
        throw new NotImplementedException();
    }

    public void VisitGetExpr(Expr.Get expr)
    {
        throw new NotImplementedException();
    }

    public void VisitSetExpr(Expr.Set expr)
    {
        throw new NotImplementedException();
    }

    public void VisitSuperExpr(Expr.Super expr)
    {
        throw new NotImplementedException();
    }

    public void VisitThisExpr(Expr.This expr)
    {
        throw new NotImplementedException();
    }

    public void VisitArrayExpr(Expr.Array expr)
    {
        throw new NotImplementedException();
    }

    public void VisitArrayAccessExpr(Expr.ArrayAccess expr)
    {
        throw new NotImplementedException();
    }

    public void VisitArrayAssignExpr(Expr.ArrayAssign expr)
    {
        throw new NotImplementedException();
    }

    public void VisitPostfixExpr(Expr.Postfix expr)
    {
        throw new NotImplementedException();
    }

    #region Utilities

    # region Variable Declaration

    #region Jumping And Looping

    private void PatchJump(int offset)
    {
        int jump = _function.Chunk.Count - offset - 2;

        if(jump > ushort.MaxValue)
        {
            AddError("Too much code to jump over.");
        }

        _function.Chunk[offset] = (byte)((jump >> 8) & 0xFF);
        _function.Chunk[offset + 1] = (byte)(jump & 0xFF);
    }

    private int EmitJump(OpCode instruction, int line)
    {
        EmitByte(instruction, line);
        EmitByte(0xFF, line);
        EmitByte(0xFF, line);
        return _function.Chunk.Count - 2;
    }

    private void EmitLoop(int loopStart, int line)
    {
        EmitByte(OpCode.Loop, line);

        int offset = _function.Chunk.Count - loopStart + 2;

        if(offset > ushort.MaxValue)
        {
            AddError("Loop body too large");
        }

        EmitByte((byte)((offset >> 8) & 0xFF), line);
        EmitByte((byte)(offset & 0xFF), line);
    }

    #endregion

    private void BeginScope() => scopeDepth++;

    private void EndScope()
    {
        scopeDepth--;

        while(localCount > 0 && _locals[localCount - 1].Depth > scopeDepth)
        {
            EmitByte(OpCode.Pop);
            localCount--;
        }
    }

    private void DefineVariable(Token name)
    {
        if(scopeDepth > 0)
        {
            MarkInitialized();
            return;
        }

        DefineGlobal(name);
    }


    private void MarkInitialized()
    {
        if(scopeDepth == 0)
        {
            return;
        }
        _locals[localCount - 1].Depth = scopeDepth;
    }

    private void DefineGlobal(Token name)
    {
        byte global = MakeConstant(LoxValue.Object(name.Lexeme));

        EmitBytes(OpCode.DefineGlobal, global, name.Line);
    }


    private void DeclareVariable(Token name)
    {
        if(scopeDepth == 0)
        {
            return;
        }

        AddLocal(name);
    }

    private void AddLocal(Token name)
    {
        if(localCount == MAX_LOCAL_COUNT)
        {
            AddError("Too many local variables in function.", name);
            return;
        }

        for(int i = localCount - 1; i >= 0; i--)
        {
            Local l = _locals[i];

            if(l.Depth != -1 && l.Depth < scopeDepth)
            {
                break;
            }

            if(l.Name == name.Lexeme)
            {
                AddError("Already a variable with this name in this scope.", name);
            }
        }

        Local local = new() { Name = name.Lexeme, Depth = -1 };
        _locals[localCount++] = local;
    }

    private int ResolveLocal(Token name)
    {
        for(int i = localCount - 1; i >= 0; i--)
        {
            Local local = _locals[i];

            if(name.Lexeme == local.Name)
            {
                if(local.Depth == -1)
                {
                    AddError("Can't read local variable in its own initializer.", name);
                }
                return i;
            }
        }

        return -1;
    }

    private void ReadGlobal(Expr.Variable expr)
    {
        Token name = expr.Name;
        byte arg = MakeConstant(LoxValue.Object(name.Lexeme));
        EmitBytes(OpCode.GetGlobal, arg, name.Line);
    }

    private void AssignGlobal(Expr.Assign expr)
    {
        byte arg = MakeConstant(LoxValue.Object(expr.Name.Lexeme));

        EmitBytes(OpCode.SetGlobal, arg, expr.Name.Line);
    }

    #endregion

    private void EmitByte(OpCode opCode) => EmitByte(opCode, latestLine);

    private void EmitByte(OpCode opCode, int line)
    {
        _function.Chunk.WriteChunk(opCode, line);
        latestLine = line;
    }

    private void EmitByte(byte val, int line)
    {
        _function.Chunk.WriteChunk(val, line);
        latestLine = line;
    }

    private void EmitBytes(OpCode opCode, byte val, int line)
    {
        EmitByte(opCode, line);
        EmitByte(val, line);
        latestLine = line;
    }
    private void EmitBytes(OpCode opCodeOne, OpCode opCodeTwo, int line) => EmitBytes(opCodeOne, line, opCodeTwo, line);

    private void EmitBytes(OpCode opCodeOne, int l1, OpCode opCodeTwo, int l2)
    {
        EmitByte(opCodeOne, l1);
        EmitByte(opCodeTwo, l2);
        latestLine = Math.Max(l1, l2);
    }

    private byte MakeConstant(LoxValue value)
    {
        int constant = _function.Chunk.AddConstant(value);
        if(constant > byte.MaxValue)
        {
            AddError("Too many constants in one chunk.");
            return 0;
        }

        return (byte)constant;
    }

    private void EmitConstant(LoxValue value, int line)
    {
        byte constant = MakeConstant(value);
        _function.Chunk.WriteChunk(OpCode.Constant, line);
        _function.Chunk.WriteChunk(constant, line);
    }

    private int GetLineNumber(Expr expr) // Try to get the line number of an Expr by checking the actual type. It would be better to save the line number in the Expr and Stmt records, but that would require many changes in the parser.
    {
        if(expr is Expr.Grouping grouping)
        {
            return GetLineNumber(grouping);
        }
        else if(expr is Expr.Logical logical)
        {
            return logical.Oper.Line;
        }
        else if(expr is Expr.Binary binary)
        {
            return binary.Operator.Line;
        }
        else if(expr is Expr.Literal literal)
        {
            return literal.Token!.Line;
        }
        else if(expr is Expr.Assign assign)
        {
            return assign.Name.Line;
        }
        else if(expr is Expr.Postfix postfix)
        {
            return postfix.Operator.Line;
        }
        else if(expr is Expr.Variable variable)
        {
            return variable.Name.Line;
        }

        throw new NotImplementedException($"{nameof(GetLineNumber)} is not implemented for {expr.GetType()}.");
    }

    private void AddError(string msg, Token? token = null) => _errors.Add(new(msg, token));

    #endregion
}

internal class Local
{
    internal required string Name { get; init; }
    internal int Depth { get; set; }
}

internal enum FunctionType
{
    Function,
    Script
}