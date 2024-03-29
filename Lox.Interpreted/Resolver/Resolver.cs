using Shared;
using Generated;
using Shared.ErrorHandling;

namespace Lox.Resolver;

internal class Resolver : Expr.IVisitor<object>, Stmt.IVisitor<object>
{
    public bool HadError => _errors.Count > 0;
    public IEnumerable<ResolveError> Errors => _errors;

    private readonly Interpreter.Interpreter _interpreter;
    private readonly Stack<Dictionary<string, bool>> _scopes = new();

    private readonly List<ResolveError> _errors;

    private FunctionType currentFunction = FunctionType.NONE;
    private ClassType _currentClass = ClassType.NONE;

    public Resolver(Interpreter.Interpreter interpreter)
    {
        _interpreter = interpreter;
        _errors = [];
    }

    public object VisitBlockStmt(Stmt.Block stmt)
    {
        BeginScope();
        Resolve(stmt.Statements);
        EndScope();
        return null!;
    }

    public object VisitVarStmt(Stmt.Var stmt)
    {
        Declare(stmt.Name);
        if(stmt.Initializer is not null)
        {
            Resolve(stmt.Initializer);
        }
        Define(stmt.Name);
        return null!;
    }

    public object VisitVariableExpr(Expr.Variable expr)
    {
        if(_scopes.Count != 0 && _scopes.Peek().TryGetValue(expr.Name.Lexeme, out bool found) && !found)
        {
            _errors.Add(new("Can't read local variable in its own initializer.", expr.Name));
        }

        ResolveLocal(expr, expr.Name);
        return null!;
    }

    public object VisitAssignExpr(Expr.Assign expr)
    {
        Resolve(expr.Value);
        ResolveLocal(expr, expr.Name);
        return null!;
    }

    public object VisitFunctionStmt(Stmt.Function stmt)
    {
        Declare(stmt.Name);
        Define(stmt.Name);

        ResolveFunction(stmt, FunctionType.FUNCTION);

        return null!;
    }

    private void ResolveFunction(Stmt.Function function, FunctionType type)
    {
        FunctionType enclosingFunction = currentFunction;
        currentFunction = type;

        BeginScope();
        foreach(Token param in function.Params)
        {
            Declare(param);
            Define(param);
        }

        Resolve(function.Body);
        EndScope();
        currentFunction = enclosingFunction;
    }

    public object VisitBinaryExpr(Expr.Binary expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
        return null!;
    }

    public object VisitCallExpr(Expr.Call expr)
    {
        Resolve(expr.Callee);

        foreach(Expr arg in expr.Arguments)
        {
            Resolve(arg);
        }

        return null!;
    }

    public object VisitGroupingExpr(Expr.Grouping expr)
    {
        Resolve(expr.Expression);
        return null!;
    }

    public object VisitLiteralExpr(Expr.Literal expr)
    {
        return null!;
    }

    public object VisitLogicalExpr(Expr.Logical expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
        return null!;
    }

    public object VisitUnaryExpr(Expr.Unary expr)
    {
        Resolve(expr.Right);
        return null!;
    }

    public object VisitExpressionStmt(Stmt.Expression stmt)
    {
        Resolve(stmt.Ex);
        return null!;
    }

    public object VisitIfStmt(Stmt.If stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.ThenBranch);
        if(stmt.ElseBranch is not null)
        {
            Resolve(stmt.ElseBranch!);
        }
        return null!;
    }

    public object VisitPrintStmt(Stmt.Print stmt)
    {
        Resolve(stmt.Ex);
        return null!;
    }

    public object VisitReturnStmt(Stmt.Return stmt)
    {
        if(currentFunction is FunctionType.NONE)
        {
            _errors.Add(new("Can't return from top-level code.", stmt.Keyword));
        }

        if(stmt.Value is not null)
        {
            if(currentFunction is FunctionType.INITIALIZER)
            {
                _errors.Add(new("Can't return value from an initializer.", stmt.Keyword));
            }
            Resolve(stmt.Value);
        }
        return null!;
    }

    public object VisitWhileStmt(Stmt.While stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.Body);
        return null!;
    }

    public object VisitForStmt(Stmt.For stmt)
    {
        BeginScope();
        if(stmt.Initializer is not null)
        {
            Resolve(stmt.Initializer);
        }
        if(stmt.Condition is not null)
        {
            Resolve(stmt.Condition);
        }
        if(stmt.Increment is not null)
        {
            Resolve(stmt.Increment);
        }

        Resolve(stmt.Body);

        EndScope();

        return null!;
    }

    public object VisitClassStmt(Stmt.Class stmt)
    {
        ClassType enclosing = _currentClass;
        _currentClass = ClassType.CLASS;

        Declare(stmt.Name);
        Define(stmt.Name);

        if(stmt.Superclass is not null && stmt.Name.Lexeme.Equals(stmt.Superclass.Name.Lexeme))
        {
            _errors.Add(new("A class can't inherit from itself.", stmt.Superclass.Name));
        }

        if(stmt.Superclass is not null)
        {
            _currentClass = ClassType.SUBCLASS;
            Resolve(stmt.Superclass);
        }

        if(stmt.Superclass is not null)
        {
            BeginScope();
            _scopes.Peek().Add("super", true);
        }

        BeginScope();
        _scopes.Peek().Add("this", true);

        foreach(Stmt.Function method in stmt.Methods)
        {
            ResolveFunction(method, method.Name.Lexeme.Equals("init") ? FunctionType.INITIALIZER : FunctionType.METHOD);
        }

        EndScope();

        if(stmt.Superclass is not null)
        {
            EndScope();
        }

        _currentClass = enclosing;
        return null!;
    }

    public object VisitGetExpr(Expr.Get expr)
    {
        Resolve(expr.Obj);
        return null!;
    }

    public object VisitSetExpr(Expr.Set expr)
    {
        Resolve(expr.Value);
        Resolve(expr.Obj);
        return null!;
    }

    public object VisitThisExpr(Expr.This expr)
    {
        if(_currentClass is ClassType.NONE)
        {
            _errors.Add(new("Can't use 'this' outside of a class.", expr.Keyword));
            return null!;
        }

        ResolveLocal(expr, expr.Keyword);
        return null!;
    }

    public object VisitSuperExpr(Expr.Super expr)
    {
        if(_currentClass is ClassType.NONE)
        {
            _errors.Add(new("Can't use 'super' outside of a class.", expr.Keyword));
        }
        else if(_currentClass is not ClassType.SUBCLASS)
        {
            _errors.Add(new("Can't use 'super' in a class with no superclass.", expr.Keyword));

        }
        ResolveLocal(expr, expr.Keyword);
        return null!;
    }

    public object VisitBreakStmt(Stmt.Break stmt)
    {
        return null!;
    }

    public object VisitContinueStmt(Stmt.Continue stmt)
    {
        return null!;
    }

    public object VisitArrayExpr(Expr.Array expr)
    {
        if(expr.Initializers is not null)
        {
            foreach(Expr e in expr.Initializers)
            {
                Resolve(e);
            }
        }
        else if(expr.DefaultValue is not null && expr.DefaultValueCount is not null)
        {
            Resolve(expr.DefaultValue);
            Resolve(expr.DefaultValueCount);
        }

        return null!;
    }

    public object VisitArrayAccessExpr(Expr.ArrayAccess expr)
    {
        Resolve(expr.Target);
        Resolve(expr.Index);

        return null!;
    }

    public object VisitArrayAssignExpr(Expr.ArrayAssign expr)
    {
        Resolve(expr.Target);
        Resolve(expr.Index);
        Resolve(expr.Value);

        return null!;
    }

    public object VisitPostfixExpr(Expr.Postfix expr)
    {
        Resolve(expr.Obj);

        return null!;
    }

    private void ResolveLocal(Expr expr, Token name)
    {
        for(int i = 0; i < _scopes.Count; i++)
        {
            if(_scopes.ElementAt(i).ContainsKey(name.Lexeme))
            {
                _interpreter.Resolve(expr, i);
                return;
            }
        }
    }

    private void Define(Token name)
    {
        if(_scopes.Count == 0)
        {
            return;
        }

        if(_scopes.Peek().ContainsKey(name.Lexeme))
        {
            _scopes.Peek()[name.Lexeme] = true;
        }
        else
        {
            _errors.Add(new($"Variable '{name.Lexeme}' has not been declared.", name));
        }
    }

    private void Declare(Token name)
    {
        if(_scopes.Count == 0)
        {
            return;
        }

        Dictionary<string, bool> scope = _scopes.Peek();
        if(scope.ContainsKey(name.Lexeme))
        {
            _errors.Add(new($"Already a variable with this name in this scope.", name));
            return;
        }
        scope.Add(name.Lexeme, false);
    }

    private void EndScope()
    {
        _scopes.Pop();
    }

    private void BeginScope()
    {
        _scopes.Push([]);
    }

    internal void Resolve(List<Stmt> statements)
    {
        foreach(Stmt stmt in statements)
        {
            Resolve(stmt);
        }
    }

    private void Resolve(Stmt stmt)
    {
        stmt.Accept(this);
    }

    private void Resolve(Expr expr)
    {
        expr.Accept(this);
    }
}

enum FunctionType
{
    NONE,
    FUNCTION,
    INITIALIZER,
    METHOD
}

enum ClassType
{
    NONE,
    CLASS,
    SUBCLASS
}