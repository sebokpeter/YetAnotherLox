
using System.Diagnostics;
using Generated;
using LoxVM.Chunk;
using LoxVM.Value;
using Shared;
using Shared.ErrorHandling;

namespace LoxVM.VM;

internal class BytecodeEmitter : Expr.IVoidVisitor, Stmt.IVoidVisitor
{
    internal bool HadError => _errors.Count > 0;
    internal IEnumerable<BytecodeEmitterError> Errors => _errors;

    private readonly List<Stmt> _statements;

    private readonly Chunk.Chunk _chunk;
    private readonly List<BytecodeEmitterError> _errors;

    private readonly Compiler _current;

    private int latestLine; // Remember the last line number we saw, so that we can use it when we want to add an instruction, but do not know the line number. (E.g. when emitting a POP instruction in EndScope())

    public BytecodeEmitter(List<Stmt> stmts)
    {
        _statements = stmts;
        _chunk = new();
        _errors = [];
        _current = new();
        latestLine = 0;
    }

    public Chunk.Chunk EmitBytecode()
    {
        // TODO
        foreach(Stmt stmt in _statements)
        {
            EmitBytecode(stmt);
        }

        // TODO: remove temporary return
        EmitByte(OpCode.Return, _statements.Count);
        return _chunk;
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
    #region Statements

    public void VisitExpressionStmt(Stmt.Expression stmt)
    {
        EmitBytecode(stmt.Ex);
        EmitByte(OpCode.Pop, stmt.Line);
    }

    public void VisitReturnStmt(Stmt.Return stmt)
    {
        throw new NotImplementedException();
        //EmitByte(OpCode.Return, stmt.Keyword.Line);
    }

    public void VisitPrintStmt(Stmt.Print stmt)
    {
        EmitBytecode(stmt.Ex);
        EmitByte(OpCode.Print, stmt.Line);
    }

    public void VisitVarStmt(Stmt.Var stmt)
    {
        if(stmt.Initializer is not null)
        {
            EmitBytecode(stmt.Initializer);
        }
        else
        {
            EmitByte(OpCode.Nil, stmt.Name.Line);
        }

        if(_current.ScopeDepth == 0)
        {
            DefineGlobal(stmt);
        }
        else
        {
            DefineLocal(stmt);
        }
    }

    public void VisitBlockStmt(Stmt.Block stmt)
    {
        BeginScope();
        EmitBytecode(stmt.Statements);
        EndScope();
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

    #endregion

    public void VisitClassStmt(Stmt.Class stmt)
    {
        throw new NotImplementedException();
    }

    public void VisitFunctionStmt(Stmt.Function stmt)
    {
        throw new NotImplementedException();
    }

    public void VisitIfStmt(Stmt.If stmt)
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

    public void VisitWhileStmt(Stmt.While stmt)
    {
        throw new NotImplementedException();
    }

    public void VisitForStmt(Stmt.For stmt)
    {
        throw new NotImplementedException();
    }

    public void VisitCallExpr(Expr.Call expr)
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

    private void BeginScope() => _current.ScopeDepth++;

    private void EndScope()
    {
        _current.ScopeDepth--;

        while(_current.LocalCount > 0 && _current.Locals[_current.LocalCount - 1].Depth > _current.ScopeDepth)
        {
            EmitByte(OpCode.Pop);
            _current.LocalCount--;
        }
    }

    private void DefineVariable(byte global, int line) => EmitBytes(OpCode.DefineGlobal, global, line);

    private void DefineGlobal(Stmt.Var stmt)
    {
        byte global = MakeConstant(LoxValue.Object(stmt.Name.Lexeme));

        DefineVariable(global, stmt.Name.Line);
    }

    private void DefineLocal(Stmt.Var stmt)
    {
        if(_current.LocalCount == Compiler.MAX_LOCAL_COUNT)
        {
            AddError("Too many local variables in function.", stmt.Name);
            return;
        }

        for(int i = _current.LocalCount - 1; i >= 0; i--)
        {
            Local l = _current.Locals[i];

            if(l.Depth < _current.ScopeDepth)
            {
                break;
            }

            if(l.Name.Lexeme == stmt.Name.Lexeme)
            {
                AddError("Already a variable with this name in this scope.", stmt.Name);
            }
        }

        Local local = new() { Name = stmt.Name, Depth = _current.ScopeDepth };
        _current.Locals[_current.LocalCount++] = local;
    }

    private int ResolveLocal(Token name)
    {
        for(int i = _current.LocalCount - 1; i >= 0; i--)
        {
            Local local = _current.Locals[i];

            if(name.Lexeme == local.Name.Lexeme)
            {
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
        _chunk.WriteChunk(opCode, line);
        latestLine = line;
    }

    private void EmitByte(byte val, int line)
    {
        _chunk.WriteChunk(val, line);
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
        int constant = _chunk.AddConstant(value);
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
        _chunk.WriteChunk(OpCode.Constant, line);
        _chunk.WriteChunk(constant, line);
    }

    private void AddError(string msg, Token? token = null) => _errors.Add(new(msg, token));

    #endregion
}

internal class Compiler
{
    internal const int MAX_LOCAL_COUNT = byte.MaxValue + 1;
    internal Local[] Locals { get; init; }
    internal int ScopeDepth { get; set; }
    internal int LocalCount { get; set; }

    public Compiler()
    {
        Locals = new Local[MAX_LOCAL_COUNT];
    }
}

internal class Local
{
    internal required Token Name { get; init; }
    internal int Depth { get; init; }
}