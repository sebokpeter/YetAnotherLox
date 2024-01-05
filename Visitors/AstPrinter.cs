using System.Text;
using Generated;

namespace LoxConsole.Visitors;

public class AstPrinter : Expr.IVisitor<string>, Stmt.IVisitor<string>
{
    public string VisitExpressionStmt(Stmt.Expression stmt)
    {
        return Parenthesize("exprStmt", stmt.Ex);
    }

    public string VisitPrintStmt(Stmt.Print stmt)
    {
        return Parenthesize("print", stmt.Ex);
    }

    public string Print(List<Stmt> stmts)
    {
        StringBuilder sb = new();

        foreach (Stmt stmt in stmts)
        {
            if(stmt is null)
            {
                continue;
            }
            sb.AppendLine(stmt.Accept(this));
        }

        return sb.ToString();
    }

    public string VisitBinaryExpr(Expr.Binary expr)
    {
        return Parenthesize(expr.Operator.Lexeme, expr.Left, expr.Right);
    }

    public string VisitGroupingExpr(Expr.Grouping expr)
    {
        return Parenthesize("group", expr.Expression);
    }

    public string VisitLiteralExpr(Expr.Literal expr)
    {
        return expr.Value is null ? "nill" : expr.Value.ToString()!;
    }

    public string VisitUnaryExpr(Expr.Unary expr)
    {
        return Parenthesize(expr.Operator.Lexeme, expr.Right);
    }

    public string VisitVariableExpr(Expr.Variable expr)
    {
        return $"(varExpr {expr.Name.Lexeme})";
    }

    public string VisitAssignExpr(Expr.Assign expr)
    {
        return Parenthesize("assign", expr.Value);
    }

    public string VisitVarStmt(Stmt.Var stmt)
    {
        if (stmt.Initializer is null) 
        {
            return Parenthesize("varDecl");
        }
        return Parenthesize("varDecl", stmt.Initializer);
    }

    private string Parenthesize(string name, params Expr[] exprs)
    {
        StringBuilder sb = new();

        sb.Append('(').Append(name);
        foreach (Expr expr in exprs)
        {
            sb.Append(' ');
            sb.Append(expr.Accept(this));
        }
        sb.Append(')');

        return sb.ToString();
    }

    public string VisitBlockStmt(Stmt.Block stmt)
    {
        string statements = Print(stmt.Statements);
        return $"(block {statements})";
    }

    public string VisitIfStmt(Stmt.If stmt)
    {
        string ifS = Parenthesize("if", stmt.Condition);
        string ifStmt = $"({ifS} (then {stmt.ThenBranch.Accept(this)}))";
        if (stmt.ElseBranch is not null) 
        {
            ifStmt += $"(else {stmt.ElseBranch.Accept(this)})";
        }   
        return ifStmt;
    }

    public string VisitLogicalExpr(Expr.Logical expr)
    {
        throw new NotImplementedException();
    }

    public string VisitWhileStmt(Stmt.While stmt)
    {
        throw new NotImplementedException();
    }

    public string VisitCallExpr(Expr.Call expr)
    {
        throw new NotImplementedException();
    }

    public string VisitFunctionStmt(Stmt.Function stmt)
    {
        throw new NotImplementedException();
    }

    public string VisitReturnStmt(Stmt.Return stmt)
    {
        throw new NotImplementedException();
    }

    public string VisitClassStmt(Stmt.Class stmt)
    {
        throw new NotImplementedException();
    }

    public string VisitGetExpr(Expr.Get expr)
    {
        throw new NotImplementedException();
    }

    public string VisitSetExpr(Expr.Set expr)
    {
        throw new NotImplementedException();
    }

    public string VisitThisExpr(Expr.This expr)
    {
        throw new NotImplementedException();
    }

    public string VisitSuperExpr(Expr.Super expr)
    {
        throw new NotImplementedException();
    }

    public string VisitBreakStmt(Stmt.Break stmt)
    {
        throw new NotImplementedException();
    }

    public string VisitForStmt(Stmt.For stmt)
    {
        throw new NotImplementedException();
    }

    public string VisitContinueStmt(Stmt.Continue stmt)
    {
        throw new NotImplementedException();
    }
}