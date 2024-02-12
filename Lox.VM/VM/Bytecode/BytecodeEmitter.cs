
using Generated;
using LoxVM.Chunk;

namespace LoxVM.VM;

internal class BytecodeEmitter : Expr.IVoidVisitor, Stmt.IVoidVisitor
{
    public bool HadError => throw new NotImplementedException();
    public IEnumerable<CompilationError> Errors => throw new NotImplementedException();

    private readonly List<Stmt> _statements;

    private Chunk.Chunk chunk;

    public BytecodeEmitter(List<Stmt> stmts)
    {
        _statements = stmts;
        chunk = new();
    }

    public Chunk.Chunk EmitBytecode()
    {
        // TODO
        foreach(Stmt stmt in _statements)
        {
            EmitBytecode(stmt);
        }
        return chunk;
    }

    private void EmitBytecode(Stmt stmt) => stmt.Accept(this);

    #region Statements


    #endregion

    #region Expressions



    #endregion

    public void VisitBlockStmt(Stmt.Block stmt)
    {
        throw new NotImplementedException();
    }

    public void VisitClassStmt(Stmt.Class stmt)
    {
        throw new NotImplementedException();
    }

    public void VisitExpressionStmt(Stmt.Expression stmt)
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

    public void VisitReturnStmt(Stmt.Return stmt)
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

    public void VisitLiteralExpr(Expr.Literal expr)
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

    public void VisitUnaryExpr(Expr.Unary expr)
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
}