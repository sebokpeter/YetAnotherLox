using Generated;
using static Lox.TokenType;

namespace Lox.Interpreter;

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
        DefineGlobalClasses();
    }

    private void DefineGlobalClasses()
    {
        _globals.Define("Math", new StaticNativeClasses.LoxMath());
    }

    private void DefineGlobalFunctions()
    {
        _globals.Define("sleep", new NativeFunction.Sleep());
        _globals.Define("clock", new NativeFunction.Clock());
        _globals.Define("random", new NativeFunction.LoxRandom());
        _globals.Define("stringify", new NativeFunction.Stringify(Stringify));
        _globals.Define("num", new NativeFunction.Num());
        _globals.Define("int", new NativeFunction.Int());
        _globals.Define("input", new NativeFunction.Input());
        _globals.Define("readFile", new NativeFunction.ReadFile());
        _globals.Define("len", new NativeFunction.Len());
        _globals.Define("write", new NativeFunction.Write(Stringify));
        _globals.Define("clear", new NativeFunction.Clear());
    }

    internal void Interpret(List<Stmt> statements)
    {
        try
        {
            foreach(Stmt stmt in statements)
            {
                Execute(stmt);
            }
        }
        catch(RuntimeException ex)
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
        if(value is null)
        {
            return "nil";
        }
        if(value is double d)
        {
            string text = d.ToString();
            if(text.EndsWith(".0"))
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
        if(stmt.Initializer is not null)
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

        switch(expr.Operator.Type)
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

        AssignToVariable(expr, value);

        return value;
    }

    public object VisitBlockStmt(Stmt.Block stmt)
    {
        ExecuteBlock(stmt.Statements, new Environment(_environment));
        return null!;
    }

    public object VisitIfStmt(Stmt.If stmt)
    {
        if(IsTruthy(Evaluate(stmt.Condition)))
        {
            Execute(stmt.ThenBranch);
        }
        else if(stmt.ElseBranch is not null)
        {
            Execute(stmt.ElseBranch);
        }
        return null!;
    }

    internal void ExecuteBlock(List<Stmt> statements, Environment environment)
    {
        Environment previous = _environment;
        try
        {
            _environment = environment;

            foreach(Stmt stmt in statements)
            {
                Execute(stmt);
            }
        }
        finally
        {
            _environment = previous;
        }
    }

    private static bool IsEqual(object left, object right)
    {
        if(left is null && right is null)
        {
            return true;
        }
        if(left is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    private static object Plus(object left, object right)
    {
        if(left is double l && right is double r)
        {
            return l + r;
        }

        string sLeft = left is string sl ? sl : Stringify(left);
        string sRight = right is string sr ? sr : Stringify(right);

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

        switch(expr.Operator.Type)
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

        if(expr.Oper.Type == OR)
        {
            if(IsTruthy(left))
            {
                return left;
            }
        }
        else
        {
            if(!IsTruthy(left))
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
            while(IsTruthy(Evaluate(stmt.Condition)))
            {
                try
                {
                    Execute(stmt.Body);
                }
                catch(Continue)
                {
                    continue;
                }
            }
        }
        catch(Break)
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
            if(stmt.Initializer is not null)
            {
                Execute(stmt.Initializer);
            }

            Expr condition = stmt.Condition ?? new Expr.Literal(true);

            try
            {
                while(IsTruthy(Evaluate(condition)))
                {
                    try
                    {
                        Execute(stmt.Body);
                    }
                    catch(Continue) { }
                    finally
                    {
                        if(stmt.Increment is not null)
                        {
                            Evaluate(stmt.Increment);
                        }
                    }
                }
            }
            catch(Break) { }
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

        if(callee is LoxStaticClass staticClass)
        {
            throw new RuntimeException(expr.Paren, "Static classes can not be instantiated.");
        }

        if(callee is not ILoxCallable function)
        {
            throw new RuntimeException(expr.Paren, "Can only call functions and classes");
        }

        if(arguments.Count != function.Arity)
        {
            throw new RuntimeException(expr.Paren, $"Expected {function.Arity} arguments, but got {arguments.Count}.");
        }

        try
        {
            return function.Call(this, arguments)!;
        }
        catch(Exception ex) when(ex is not RuntimeException)
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
        if(stmt.Value is not null)
        {
            value = Evaluate(stmt.Value);
        }

        throw new Return(value);
    }

    public object VisitClassStmt(Stmt.Class stmt)
    {
        object? superclass = null;
        if(stmt.Superclass is not null)
        {
            superclass = Evaluate(stmt.Superclass);
            if(superclass is not LoxNonStaticClass)
            {
                throw new RuntimeException(stmt.Superclass.Name, "Superclass must be a (non-static) class.");
            }
        }

        _environment.Define(stmt.Name.Lexeme, null!);

        if(stmt.Superclass is not null)
        {
            _environment = new(_environment);
            _environment.Define("super", superclass!);
        }

        Dictionary<string, LoxFunction> methods = [];

        foreach(Stmt.Function method in stmt.Methods)
        {
            LoxFunction function = new(method, _environment, method.Name.Lexeme.Equals("init"));
            methods.Add(method.Name.Lexeme, function);
        }

        LoxClass @class = stmt.IsStatic ? new LoxStaticClass(stmt.Name.Lexeme, methods) : new LoxNonStaticClass(stmt.Name.Lexeme, (LoxNonStaticClass?)superclass, methods);

        if(superclass is not null)
        {
            _environment = _environment.Enclosing!;
        }

        _environment.Assign(stmt.Name, @class);
        return null!;
    }

    public object VisitGetExpr(Expr.Get expr)
    {
        object obj = Evaluate(expr.Obj);
        if(obj is LoxInstance instance)
        {
            return instance.Get(expr.Name)!;
        }

        if(obj is LoxClass @class)
        {
            LoxFunction? func = @class.FindMethod(expr.Name.Lexeme);
            if(func is null || !func.IsStatic)
            {
                throw new RuntimeException(expr.Name, $"Class '{@class.Name}' has no static method named '{expr.Name.Lexeme}'.");
            }

            return func;
        }

        throw new RuntimeException(expr.Name, "Only instances have properties.");
    }

    public object VisitSetExpr(Expr.Set expr)
    {
        object obj = Evaluate(expr.Obj);

        if(obj is not LoxInstance instance)
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
        LoxNonStaticClass superclass = (LoxNonStaticClass)_environment.GetAt(distance, "super");
        LoxInstance obj = (LoxInstance)_environment.GetAt(distance - 1, "this");

        LoxFunction? method = superclass.FindMethod(expr.Method.Lexeme) ?? throw new RuntimeException(expr.Method, $"Undefined property '{expr.Method.Lexeme}'.");
        return method.Bind(obj);
    }

    public object VisitBreakStmt(Stmt.Break stmt) => throw new Break();

    public object VisitContinueStmt(Stmt.Continue stmt) => throw new Continue();

    public object VisitArrayExpr(Expr.Array expr)
    {
        List<object> values;
        if(expr.Initializers is not null)
        {
            values = expr.Initializers.Select(Evaluate).ToList();
        }
        else if(expr.DefaultValue is not null && expr.DefaultValueCount is not null)
        {
            object defValCount = Evaluate(expr.DefaultValueCount);

            if(defValCount is not double d || d % 1 != 0)
            {
                throw new RuntimeException(expr.Bracket, "Default value count must be an integer.");
            }

            int defaultValueCount = Convert.ToInt32(d);

            values = [];
            for(int i = 0; i < defaultValueCount; i++)
            {
                values.Add(Evaluate(expr.DefaultValue));
            }
        }
        else
        {
            values = [];
        }

        return new LoxArray(values);
    }

    public object VisitArrayAccessExpr(Expr.ArrayAccess expr)
    {
        object target = Evaluate(expr.Target);

        if(target is not (LoxArray or string))
        {
            throw new RuntimeException(expr.Bracket, "Expected array or string.");
        }

        object location = Evaluate(expr.Index);

        if(location is not double loc || loc % 1 != 0)
        {
            throw new RuntimeException(expr.Bracket, "Location must be an integer.");
        }

        int targetLocation = (int)loc;

        if(target is string str)
        {
            if(targetLocation >= str.Length)
            {
                throw new RuntimeException(expr.Bracket, "Requested location is higher than the length of the string.");
            }
            return str[targetLocation].ToString();
        }
        else
        {
            return ((LoxArray)target).Get(targetLocation, expr.Bracket);
        }
    }

    public object VisitArrayAssignExpr(Expr.ArrayAssign expr)
    {
        object target = Evaluate(expr.Target);

        if(target is not LoxArray array)
        {
            throw new RuntimeException(expr.Bracket, "Expected array.");
        }

        object location = Evaluate(expr.Index);

        if(location is not double loc || loc % 1 != 0)
        {
            throw new RuntimeException(expr.Bracket, "Location must be an integer.");
        }

        int targetLocation = (int)loc;
        object value = Evaluate(expr.Value);

        array.Assign(targetLocation, value);

        return null!;
    }

    public object VisitPostfixExpr(Expr.Postfix expr)
    {
        double ApplyPostfix(double num)
        {
            return expr.Operator.Type == PLUS_PLUS ? num + 1 : num - 1;
        }

        double CheckNumber(object? o, Token name)
        {
            if(o is null || o is not double d)
            {
                throw new RuntimeException(name, "Can only apply postfix operator to a number.");
            }
            return d;
        }

        if(expr.Obj is Expr.Variable varExpr)
        {
            // Postfix expression is applied to a variable
            // Get the current value and check if it is a number
            object current = LookupVariable(varExpr.Name, varExpr);

            double currentDouble = CheckNumber(current, varExpr.Name);

            double newValue = ApplyPostfix(currentDouble);

            // Same as in VisitAssignExpr(), assign the new value to the variable
            // But here the type of the Expr is Expr.Variable
            AssignToVariable(varExpr, newValue);

            // Return the old value to conform to postfix semantics
            return currentDouble;
        }
        else if(expr.Obj is Expr.Get getExpr)
        {
            // Postfix expression is applied to a getter
            object obj = Evaluate(getExpr.Obj);

            // Similar to VisitGetExpr();
            if(obj is LoxInstance instance)
            {
                object? current = instance.Get(getExpr.Name);

                double currentDouble = CheckNumber(current, getExpr.Name);

                double newValue = ApplyPostfix(currentDouble);

                // Set the new value in the instance
                instance.Set(getExpr.Name, newValue);

                // But still return the old value
                return currentDouble;
            }
            else
            {
                // Parser should take care of this case
                throw new RuntimeException(expr.Operator, "Can only apply postfix operator to a variable or getter.");
            }
        }
        else
        {
            // Parser should also take care of this case
            throw new RuntimeException(expr.Operator, "Can only apply postfix operator to a variable or getter.");
        }
    }


    private void AssignToVariable(Expr.Assign expr, object newValue)
    {
        if(_locals.TryGetValue(expr, out int distance))
        {
            _environment.AssignAt(distance, expr.Name, newValue);
        }
        else
        {
            _globals.Assign(expr.Name, newValue);
        }
    }

    private void AssignToVariable(Expr.Variable expr, object newValue)
    {
        if(_locals.TryGetValue(expr, out int distance))
        {
            _environment.AssignAt(distance, expr.Name, newValue);
        }
        else
        {
            _globals.Assign(expr.Name, newValue);
        }
    }

    private static void CheckNumberOperand(Token @operator, object operand)
    {
        if(operand is double) return;
        throw new RuntimeException(@operator, "Operand must be a number.");
    }

    private static void CheckNumberOperands(Token @operator, object left, object right)
    {
        if(left is double && right is double) return;
        throw new RuntimeException(@operator, "Operands must be numbers");
    }

    private static bool IsTruthy(object obj)
    {
        if(obj is null)
        {
            return false;
        }
        if(obj is bool b)
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
        if(_locals.TryGetValue(expr, out int distance))
        {
            return _environment.GetAt(distance, name.Lexeme);
        }
        else
        {
            return _globals.Get(name);
        }
    }
}
