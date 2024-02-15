
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

    public BytecodeEmitter(List<Stmt> stmts)
    {
        _statements = stmts;
        _chunk = new();
        _errors = [];
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

    #region Statements

    public void VisitExpressionStmt(Stmt.Expression stmt)
    {
        EmitBytecode(stmt.Ex);
    }

    public void VisitReturnStmt(Stmt.Return stmt)
    {
        EmitByte(OpCode.Return, stmt.Keyword.Line);
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

        switch (expr.Operator.Type)
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

    #endregion

    public void VisitBlockStmt(Stmt.Block stmt)
    {
        throw new NotImplementedException();
    }

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

    public void VisitPrintStmt(Stmt.Print stmt)
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

    public void VisitVarStmt(Stmt.Var stmt)
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

    public void VisitAssignExpr(Expr.Assign expr)
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

    public void VisitLogicalExpr(Expr.Logical expr)
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

    public void VisitVariableExpr(Expr.Variable expr)
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

    private void EmitByte(OpCode opCode, int line)
    {
        _chunk.WriteChunk(opCode, line);
    }

    private void EmitBytes(OpCode opCodeOne, OpCode opCodeTwo, int line) => EmitBytes(opCodeOne, line, opCodeTwo, line);

    private void EmitBytes(OpCode opCodeOne, int l1, OpCode opCodeTwo, int l2)
    {
        EmitByte(opCodeOne, l1);
        EmitByte(opCodeTwo, l2);
    }

    private void EmitConstant(LoxValue value, int line)
    {
        int constant = _chunk.AddConstant(value);
        _chunk.WriteChunk(OpCode.Constant, line);
        _chunk.WriteChunk((byte)constant, line);
    }

    #endregion
}