using System.Diagnostics;
using System.Runtime.Intrinsics.Arm;
using Generated;
using static LoxConsole.TokenType;

namespace LoxConsole.Interpreter;

internal class Interpreter : Expr.IVisitor<object>, Stmt.IVisitor<object>
{
    private readonly Dictionary<Expr, int> _locals;
    private readonly Environment _globals;
    private Environment _environment;

    internal Environment Globals => _globals;


    internal Interpreter()
    {
        _locals = [];
        _globals = new();

        DefineGlobals();

        _environment = _globals;
    }

    private void DefineGlobals()
    {
        DefineGlobalFunctions();
    }

    private void DefineGlobalFunctions()
    {
        _globals.Define("sleep", new NativeFunction.Sleep());
        _globals.Define("clock", new NativeFunction.Clock());
        _globals.Define("random", new NativeFunction.LoxRandom());
        _globals.Define("stringify", new NativeFunction.Stringify(Stringify));
        _globals.Define("num", new NativeFunction.Num());
        _globals.Define("input", new NativeFunction.Input());
        _globals.Define("readFile", new NativeFunction.ReadFile());
    }

    internal void Interpret(List<Stmt> statements)
    {
        try
        {
            foreach (Stmt stmt in statements)
            {
                Execute(stmt);
            }
        }
        catch (RuntimeException ex)
        {
            Lox.RuntimeError(ex);
        }
    }

    private void Execute(Stmt stmt)
    {
        stmt.Accept(this);
    }

    private static string Stringify(object? value)
    {
        if (value is null)
        {
            return "nil";
        }
        if (value is double d)
        {
            string text = d.ToString();
            if (text.EndsWith(".0"))
            {
                text = text[..^2];
            }
            return text;
        }

        return value.ToString()!;
    }

    public object VisitExpressionStmt(Stmt.Expression stmt)
    {
        Evaluate(stmt.Ex);
        return null!;
    }

    public object VisitPrintStmt(Stmt.Print stmt)
    {
        object value = Evaluate(stmt.Ex);
        Console.WriteLine(Stringify(value));
        return null!;
    }

    public object VisitVarStmt(Stmt.Var stmt)
    {
        object value = null!;
        if (stmt.Initializer is not null)
        {
            value = Evaluate(stmt.Initializer);
        }

        _environment.Define(stmt.Name.Lexeme, value);
        return null!;
    }

    public object VisitBinaryExpr(Expr.Binary expr)
    {
        object left = Evaluate(expr.Left);
        object right = Evaluate(expr.Right);

        switch (expr.Operator.Type)
        {
            case MINUS:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left - (double)right;
            case SLASH:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left / (double)right;
            case STAR:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left * (double)right;
            case MODULO:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left % (double)right;
            case GREATER:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left > (double)right;
            case GREATER_EQUAL:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left >= (double)right;
            case LESS:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left < (double)right;
            case LESS_EQUAL:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left <= (double)right;
            case EQUAL_EQUAL:
                return IsEqual(left, right);
            case PLUS:
                return Plus(left, right);
        }

        return null!;
    }

    public object VisitAssignExpr(Expr.Assign expr)
    {
        object value = Evaluate(expr.Value);

        if (_locals.TryGetValue(expr, out int distance))
        {
            _environment.AssignAt(distance, expr.Name, value);
        }
        else
        {
            _globals.Assign(expr.Name, value);
        }

        return value;
    }

    public object VisitBlockStmt(Stmt.Block stmt)
    {
        ExecuteBlock(stmt.Statements, new Environment(_environment));
        return null!;
    }

    public object VisitIfStmt(Stmt.If stmt)
    {
        if (IsTruthy(Evaluate(stmt.Condition)))
        {
            Execute(stmt.ThenBranch);
        }
        else if (stmt.ElseBranch is not null)
        {
            Execute(stmt.ElseBranch);
        }
        return null!;
    }

    internal void ExecuteBlock(List<Stmt> statements, Environment environment)
    {
        Environment previous = this._environment;
        try
        {
            this._environment = environment;

            foreach (Stmt stmt in statements)
            {
                Execute(stmt);
            }
        }
        finally
        {
            this._environment = previous;
        }
    }

    private static bool IsEqual(object left, object right)
    {
        if (left is null && right is null)
        {
            return true;
        }
        if (left is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    private static object Plus(object left, object right)
    {
        if (left is double l && right is double r)
        {
            return l + r;
        }

        string sLeft = left is string sl? sl : Stringify(left);
        string sRight = right is string sr? sr : Stringify(right);

        return sLeft + sRight; 
    }

    public object VisitGroupingExpr(Expr.Grouping expr)
    {
        return Evaluate(expr.Expression);
    }

    public object VisitLiteralExpr(Expr.Literal expr)
    {
        return expr.Value!;
    }

    public object VisitUnaryExpr(Expr.Unary expr)
    {
        object right = Evaluate(expr.Right);

        switch (expr.Operator.Type)
        {
            case MINUS:
                CheckNumberOperand(expr.Operator, right);
                return -(double)right;
            case BANG:
                return !IsTruthy(right);
        }

        return null!;
    }

    public object VisitVariableExpr(Expr.Variable expr)
    {
        return LookupVariable(expr.Name, expr);
    }

    public object VisitLogicalExpr(Expr.Logical expr)
    {
        object left = Evaluate(expr.Left);

        if (expr.Oper.Type == OR)
        {
            if (IsTruthy(left))
            {
                return left;
            }
        }
        else
        {
            if (!IsTruthy(left))
            {
                return left;
            }
        }

        return Evaluate(expr.Right);
    }

    public object VisitWhileStmt(Stmt.While stmt)
    {
        try
        {
            while (IsTruthy(Evaluate(stmt.Condition)))
            {
                try
                {
                    Execute(stmt.Body);
                }
                catch (Continue)
                {
                    continue;
                }
            }
        }
        catch (Break)
        { }


        return null!;
    }

    public object VisitForStmt(Stmt.For stmt)
    {
        ExecuteForStmt(stmt, new(_environment));
        return null!;
    }

    private void ExecuteForStmt(Stmt.For stmt, Environment environment)
    {
        Environment previous = _environment;
        _environment = environment;
        try
        {
            if (stmt.Initializer is not null)
            {
                Execute(stmt.Initializer);
            }

            Expr condition = stmt.Condition ?? new Expr.Literal(true);

            try
            {
                while (IsTruthy(Evaluate(condition)))
                {
                    try
                    {
                        Execute(stmt.Body);
                    }
                    catch (Continue) { }
                    finally
                    {
                        if (stmt.Increment is not null)
                        {
                            Evaluate(stmt.Increment);
                        }
                    }
                }
            }
            catch (Break) { }
        }
        finally
        {
            _environment = previous;
        }
    }

    public object VisitCallExpr(Expr.Call expr)
    {
        object callee = Evaluate(expr.Callee);

        List<object> arguments = expr.Arguments.Select(Evaluate).ToList();

        if (callee is not ILoxCallable function)
        {
            throw new RuntimeException(expr.Paren, "Can only call functions and classes");
        }

        if (arguments.Count != function.Arity)
        {
            throw new RuntimeException(expr.Paren, $"Expected {function.Arity} arguments, but got {arguments.Count}.");
        }

        try
        {
            return function.Call(this, arguments);
        }
        catch (RuntimeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeException(expr.Paren, ex.Message);
        }
    }

    public object VisitFunctionStmt(Stmt.Function stmt)
    {
        LoxFunction function = new(stmt, _environment, false);
        _environment.Define(stmt.Name.Lexeme, function);
        return null!;
    }

    public object VisitReturnStmt(Stmt.Return stmt)
    {
        object value = null!;
        if (stmt.Value is not null)
        {
            value = Evaluate(stmt.Value);
        }

        throw new Return(value);
    }

    public object VisitClassStmt(Stmt.Class stmt)
    {
        object? superclass = null;
        if (stmt.Superclass is not null)
        {
            superclass = Evaluate(stmt.Superclass);
            if (superclass is not LoxClass)
            {
                throw new RuntimeException(stmt.Superclass.Name, "Superclass must be a class.");
            }
        }

        _environment.Define(stmt.Name.Lexeme, null!);

        if (stmt.Superclass is not null)
        {
            _environment = new(_environment);
            _environment.Define("super", superclass!);
        }

        Dictionary<string, LoxFunction> methods = [];

        foreach (Stmt.Function method in stmt.Methods)
        {
            LoxFunction function = new(method, _environment, method.Name.Lexeme.Equals("init"));
            methods.Add(method.Name.Lexeme, function);
        }

        LoxClass @class = new(stmt.Name.Lexeme, (LoxClass?)superclass, methods);

        if (superclass is not null)
        {
            _environment = _environment.Enclosing!;
        }

        _environment.Assign(stmt.Name, @class);
        return null!;
    }

    public object VisitGetExpr(Expr.Get expr)
    {
        object obj = Evaluate(expr.Obj);
        if (obj is LoxInstance instance)
        {
            return instance.Get(expr.Name)!;
        }

        throw new RuntimeException(expr.Name, "Only instances have properties.");
    }

    public object VisitSetExpr(Expr.Set expr)
    {
#if DEBUG
        if (expr.Name.Line == 34) Debugger.Break();
#endif

        object obj = Evaluate(expr.Obj);

        if (obj is not LoxInstance instance)
        {
            throw new RuntimeException(expr.Name, "Only instances have fields.");
        }

        object value = Evaluate(expr.Value);
        instance.Set(expr.Name, value);
        return value;
    }

    public object VisitThisExpr(Expr.This expr)
    {
        return LookupVariable(expr.Keyword, expr);
    }

    public object VisitSuperExpr(Expr.Super expr)
    {
        int distance = _locals[expr];
        LoxClass superclass = (LoxClass)_environment.GetAt(distance, "super");
        LoxInstance obj = (LoxInstance)_environment.GetAt(distance - 1, "this");

        LoxFunction? method = superclass.FindMethod(expr.Method.Lexeme) ?? throw new RuntimeException(expr.Method, $"Undefined property '{expr.Method.Lexeme}'.");
        return method.Bind(obj);
    }

    public object VisitBreakStmt(Stmt.Break stmt) => throw new Break();

    public object VisitContinueStmt(Stmt.Continue stmt) => throw new Continue();
    private static void CheckNumberOperand(Token @operator, object operand)
    {
        if (operand is double) return;
        throw new RuntimeException(@operator, "Operand must be a number.");
    }

    private static void CheckNumberOperands(Token @operator, object left, object right)
    {
        if (left is double && right is double) return;
        throw new RuntimeException(@operator, "Operands must be numbers");
    }

    private static bool IsTruthy(object obj)
    {
        if (obj is null)
        {
            return false;
        }
        if (obj is bool b)
        {
            return b;
        }
        return true;
    }

    private object Evaluate(Expr expression)
    {
        return expression.Accept(this);
    }

    internal void Resolve(Expr expr, int depth)
    {
        _locals.Add(expr, depth);
    }

    private object LookupVariable(Token name, Expr expr)
    {
        if (_locals.TryGetValue(expr, out int distance))
        {
            return _environment.GetAt(distance, name.Lexeme);
        }
        else
        {
            return _globals.Get(name);
        }
    }
}
