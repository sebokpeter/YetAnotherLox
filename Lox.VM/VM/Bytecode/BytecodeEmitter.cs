
using System.Diagnostics;
using Generated;
using LoxVM.Chunk;
using Shared;

namespace LoxVM.VM;

internal class BytecodeEmitter : Expr.IVoidVisitor, Stmt.IVoidVisitor
{
    public bool HadError => throw new NotImplementedException();
    public IEnumerable<CompilationError> Errors => throw new NotImplementedException();

    private readonly List<Stmt> _statements;

    private readonly Chunk.Chunk _chunk;

    public BytecodeEmitter(List<Stmt> stmts)
    {
        _statements = stmts;
        _chunk = new();
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

    private void EmitBytecode(Stmt stmt) => stmt.Accept(this);
    private void EmitBytecode(Expr expr) => expr.Accept(this);

    #region Statements

    public void VisitExpressionStmt(Stmt.Expression stmt) => EmitBytecode(stmt.Ex);

    public void VisitReturnStmt(Stmt.Return stmt) => EmitByte(OpCode.Return, stmt.Keyword.Line);

    #endregion

    #region Expressions

    public void VisitUnaryExpr(Expr.Unary expr)
    {
        EmitBytecode(expr.Right);
        switch(expr.Operator.Type)
        {
            case TokenType.BANG:
                throw new NotImplementedException();
            case TokenType.MINUS:
                EmitByte(OpCode.Negate, expr.Operator.Line);
                break;
            default:
                throw new UnreachableException();
        }
    }

    public void VisitLiteralExpr(Expr.Literal expr)
    {
        if(expr.Value is double d)
        {
            Value.Value val = new(d);
            EmitConstant(val, 1);
        }
        else
        {
            throw new NotImplementedException();
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

    public void VisitBinaryExpr(Expr.Binary expr)
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

    public void VisitGroupingExpr(Expr.Grouping expr)
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

    private void EmitByte(OpCode opCode, int line) => _chunk.WriteChunk(opCode, line);

    private void EmitBytes(OpCode opCodeOne, int l1, OpCode opCodeTwo, int l2)
    {
        EmitByte(opCodeOne, l1);
        EmitByte(opCodeTwo, l2);
    }

    private void EmitConstant(Value.Value value, int line)
    {
        int constant = _chunk.AddConstant(value);
        _chunk.WriteChunk(OpCode.Constant, line);
        _chunk.WriteChunk((byte)constant, line);
    }

    #endregion
}