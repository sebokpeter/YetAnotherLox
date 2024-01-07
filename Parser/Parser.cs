using System.Diagnostics;
using Generated;
using static LoxConsole.TokenType;

namespace LoxConsole.Parser;

internal class Parser
{
    private readonly List<Token> _tokens;
    private int _current = 0;

    private int _loopDepth = 0;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public List<Stmt> Parse()
    {
        List<Stmt> statements = [];
        while(!IsAtEnd())
        {
            statements.Add(Declaration());
        }
        return statements;
    }

    private Stmt Declaration()
    {
        try
        {
            if(Match(CLASS))
            {
                return ClassDeclaration();
            }
            if(Match(FUN))
            {
                return Function(CallableKind.FUNCTION);
            }
            if(Match(VAR))
            {
                return VarDeclaration();
            }

            return Statement();
        }
        catch(ParseException)
        {
            Synchronize();
            return null!;
        }
    }

    private Stmt.Class ClassDeclaration()
    {
        Token name = Consume(IDENTIFIER, "Expect class name.");

        Expr.Variable? superclass = null;
        if(Match(LESS))
        {
            Consume(IDENTIFIER, "Expect superclass name.");
            superclass = new Expr.Variable(Previous());
        }

        Consume(LEFT_BRACE, "Expect '{' before class body.");

        List<Stmt.Function> methods = [];

        while(!Check(RIGHT_BRACE) && !IsAtEnd()) 
        {
            methods.Add(Function(CallableKind.METHOD));
        }

        Consume(RIGHT_BRACE, "Expect '}' after class body.");

        return new Stmt.Class(name, superclass, methods);
    }

    private Stmt.Function Function(CallableKind kind)
    {
        string kindStr = kind.ToString().ToLowerInvariant();
        Token name = Consume(IDENTIFIER, $"Expect {kindStr} name.");

        Consume(LEFT_PAREN, $"Expect '(' after {kindStr} name.");
        List<Token> parameters = [];
        if (!Check(RIGHT_PAREN))
        {
            do
            {
                if (parameters.Count > 255)
                {
                    Error(Peek(), "Can't have more than 255 parameters");
                }

                parameters.Add(Consume(IDENTIFIER, "Expect parameter name"));
            } while (Match(COMMA));
        }

        Consume(RIGHT_PAREN, "Expect ')' after parameters");

        Consume(LEFT_BRACE, $"Expect '{{' before {kindStr} body");
        List<Stmt> body = Block();

        return new Stmt.Function(name, parameters, body);
    }

    private Stmt.Var VarDeclaration()
    {
        Token name = Consume(IDENTIFIER, "Expect variable name");
        Expr? initializer = null;

        if (Match(EQUAL))
        {
            initializer = Expression();
        }

        Consume(SEMICOLON, "Expect ';' after variable declaration");
        return new Stmt.Var(name, initializer);
    }

    private Stmt Statement()
    {
        if(Match(FOR))
        {
            return ForStatement();
        }
        if(Match(IF))
        {
            return IfStatement();
        }
        if(Match(PRINT))
        {
            return PrintStatement();
        }
        if(Match(RETURN))
        {
            return ReturnStatement();
        }
        if(Match(BREAK))
        {
            return BreakStatement();
        }
        if(Match(CONTINUE))
        {
            return ContinueStatement();
        }
        if(Match(WHILE))
        {
            return WhileStatement();
        }
        if(Match(LEFT_BRACE))
        {
            return new Stmt.Block(Block());
        }

        return ExpressionStatement();
    }

    private Stmt.Continue ContinueStatement()
    {
        Token keyword = Previous();

        if(_loopDepth == 0) 
        {
            Lox.Error(keyword, "Must be inside a loop to use 'continue'");
        }

        Consume(SEMICOLON, "Expect ';' after 'continue'.");

        return new Stmt.Continue(keyword);

    }

    private Stmt.Break BreakStatement()
    {
        Token keyword = Previous();

        if(_loopDepth == 0) 
        {
            Lox.Error(keyword, "Must be inside a loop to use 'break'");
        }

        Consume(SEMICOLON, "Expect ';' after 'break'.");

        return new Stmt.Break(keyword);
    }

    private Stmt.Return ReturnStatement()
    {
        Token keyword = Previous();
        Expr value = null!;

        if(!Check(SEMICOLON))
        {
            value = Expression();
        }

        Consume(SEMICOLON, "Expect ';' after return value");
        return new Stmt.Return(keyword, value);
    }

    private Stmt.For ForStatement()
    {
        Consume(LEFT_PAREN, "Expect '(' after 'for'");

        Stmt initializer;
        if (Match(SEMICOLON))
        {
            initializer = null!;
        }
        else if (Match(VAR))
        {
            initializer = VarDeclaration();
        }
        else
        {
            initializer = ExpressionStatement();
        }

        Expr condition = null!;
        if (!Check(SEMICOLON))
        {
            condition = Expression();
        }
        Consume(SEMICOLON, "Expect ';' after loop condition.");

        Expr increment = null!;
        if (!Check(RIGHT_PAREN))
        {
            increment = Expression();
        }
        Consume(RIGHT_PAREN, "Expect ')' after for clause.");

        try
        {
            _loopDepth++;
            Stmt body = Statement();

            return new Stmt.For(initializer, condition, increment, body);
        }
        finally
        {
            _loopDepth--;
        }
    }

    private Stmt.While WhileStatement()
    {
        Consume(LEFT_PAREN, "Expect '(' after 'while'");

        Expr condition = Expression();

        Consume(RIGHT_PAREN, "Expect ')' after condition");

        try
        {
            _loopDepth++;
            Stmt body = Statement();
            
            return new Stmt.While(condition, body);
        }
        finally
        {
            _loopDepth--;
        }

    }

    private Stmt.If IfStatement()
    {
        Consume(LEFT_PAREN, "Expect '(' after 'if'");
        Expr condition = Expression();
        Consume(RIGHT_PAREN, "Expect ')' after condition");

        Stmt thenBranch = Statement();
        Stmt elseBranch = null!;

        if (Match(ELSE))
        {
            elseBranch = Statement();
        }

        return new Stmt.If(condition, thenBranch, elseBranch);
    }

    private List<Stmt> Block()
    {
        List<Stmt> statements = [];

        while (!Check(RIGHT_BRACE) && !IsAtEnd())
        {
            statements.Add(Declaration());
        }

        Consume(RIGHT_BRACE, "Expect '}' after block");
        return statements;
    }

    private Stmt.Expression ExpressionStatement()
    {
        Expr value = Expression();
        Consume(SEMICOLON, "Expect ';' after value");
        return new Stmt.Expression(value);
    }

    private Stmt.Print PrintStatement()
    {
        Expr value = Expression();
        Consume(SEMICOLON, "Expect ';' after value");
        return new Stmt.Print(value);
    }

    private Expr Expression()
    {
        return Assignment();
    }

    private Expr Assignment()
    {
        Expr expr = Or();

        if (Match(EQUAL))
        {
            Token equals = Previous();
            Expr value = Assignment();

            if (expr is Expr.Variable var)
            {
                Token name = var.Name;
                return new Expr.Assign(name, value);
            } 
            else if (expr is Expr.Get getExpr)
            {
                return new Expr.Set(getExpr.Obj, getExpr.Name, value);
            }
            else if (expr is Expr.ArrayAccess arrayAccessExpr)
            {
                return new Expr.ArrayAssign(arrayAccessExpr.Target, arrayAccessExpr.Bracket, arrayAccessExpr.Location, value);
            }

            Error(equals, "Invalid assignment target");
        }

        return expr;
    }

    private Expr Or()
    {
        Expr expr = And();

        while (Match(OR))
        {
            Token oper = Previous();
            Expr right = And();
            expr = new Expr.Logical(expr, oper, right);
        }

        return expr;
    }

    private Expr And()
    {
        Expr expr = Equality();

        while (Match(AND))
        {
            Token oper = Previous();
            Expr right = Equality();
            expr = new Expr.Logical(expr, oper, right);
        }

        return expr;
    }

    private Expr Equality()
    {
        Expr expr = Comparison();

        while (Match(BANG_EQUAL, EQUAL_EQUAL))
        {
            Token oper = Previous();
            Expr right = Comparison();
            expr = new Expr.Binary(expr, oper, right);
        }

        return expr;
    }

    private Expr Comparison()
    {
        Expr expr = Term();

        while (Match(GREATER, GREATER_EQUAL, LESS, LESS_EQUAL))
        {
            Token oper = Previous();
            Expr right = Term();
            expr = new Expr.Binary(expr, oper, right);
        }

        return expr;
    }

    private Expr Term()
    {
        Expr expr = Factor();

        while (Match(MINUS, PLUS))
        {
            Token oper = Previous();
            Expr right = Factor();
            expr = new Expr.Binary(expr, oper, right);
        }

        return expr;
    }

    private Expr Factor()
    {
        Expr expr = Unary();

        while (Match(SLASH, STAR, MODULO))
        {
            Token oper = Previous();
            Expr right = Unary();
            expr = new Expr.Binary(expr, oper, right);
        }

        return expr;
    }

    private Expr Unary()
    {
        if (Match(MINUS, BANG))
        {
            Token oper = Previous();
            Expr expr = Unary();
            return new Expr.Unary(oper, expr);
        }

        return ArrayAccess();
    }

    private Expr ArrayAccess()
    {
        Expr expr = Call();

        while(Match(LEFT_SQUARE))
        {
            Token brace = Previous();
            Expr element = Expression();
            Consume(RIGHT_SQUARE, "Expect closing ']' after array access.");
            expr = new Expr.ArrayAccess(expr, brace, element);
        }

        return expr;
    }

    private Expr Call()
    {
        Expr expr = Primary();

        while (true)
        {
            if(Match(LEFT_PAREN))
            {
                expr = FinishCall(expr);
            }
            else if(Match(DOT))
            {
                Token name = Consume(IDENTIFIER, "Expect property name after '.'.");
                expr = new Expr.Get(expr, name);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private Expr.Call FinishCall(Expr callee)
    {
        List<Expr> arguments = [];

        if (!Check(RIGHT_PAREN))
        {
            do
            {
                if (arguments.Count > 255)
                {
                    Error(Peek(), "Can't have more than 255 arguments.");
                }
                arguments.Add(Expression());
            } while (Match(COMMA));
        }

        Token paren = Consume(RIGHT_PAREN, "Expect ')' after arguments.");

        return new Expr.Call(callee, paren, arguments);
    }

    private Expr Primary()
    {
        if(Match(FALSE)) return new Expr.Literal(false);
        if(Match(TRUE)) return new Expr.Literal(true);
        if(Match(NIL)) return new Expr.Literal(null);
        if(Match(NUMBER, STRING)) return new Expr.Literal(Previous().Literal);

        if(Match(SUPER))
        {
            Token keyword = Previous();
            Consume(DOT, "Expect '.' after 'super'.");
            Token method = Consume(IDENTIFIER, "Expect superclass method name.");
            return new Expr.Super(keyword, method);
        } 

        if(Match(LEFT_PAREN))
        {
            Expr expr = Expression();
            Consume(RIGHT_PAREN, "Expected ')' after expression.");
            return new Expr.Grouping(expr);
        }

        if(Match(LEFT_SQUARE))
        {
            return ArrayCreation();
        }

        if(Match(THIS)) return new Expr.This(Previous());

        if(Match(IDENTIFIER))
        {
            return new Expr.Variable(Previous());
        }

        throw Error(Peek(), "Expected expression");
    }

    private Expr.Array ArrayCreation()
    {
        Token leftSquare = Previous();
       
        if(!Check(RIGHT_SQUARE) && !IsAtEnd()) 
        {

            // If the next token is a semicolon ";", this expr will be the length of the array (e.g. [10; "a"] -> array with ten 'a's)
            // If the next token is a comma ",", this expression is the first value in the list of array value initializers (e.g. [10,11,12] -> three element array, with elements 10, 11, and 12)
            Expr first = Expression(); 

            if(Match(SEMICOLON))
            {
                Expr defaultValueCount = Expression();
                Consume(RIGHT_SQUARE, "Expect closing ']'.");
                return new Expr.Array(leftSquare, null, first, defaultValueCount);
            } 
            else if(Match(COMMA))
            {
                List<Expr> initializers = [first];
                do
                {
                    initializers.Add(Expression());
                } while (Match(COMMA));
                Consume(RIGHT_SQUARE, "Expect closing ']'.");
                return new Expr.Array(leftSquare, initializers, null, null);
            }
            else
            {
                Error(Peek(), "Expected ',' or ';'.");
            }

        }

        Consume(RIGHT_SQUARE, "Expect closing ']'.");
        return new Expr.Array(leftSquare, [], null, null);
    }

    private Token Consume(TokenType type, string msg)
    {
        if (Check(type))
        {
            return Advance();
        }

        throw Error(Peek(), msg);
    }

    private bool Match(params TokenType[] types)
    {
        foreach (TokenType type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }

        return false;
    }

    private Token Advance()
    {
        if (!IsAtEnd())
        {
            _current++;
        }
        return Previous();
    }

    private bool Check(TokenType type)
    {
        if (IsAtEnd())
        {
            return false;
        }
        return Peek().Type == type;
    }

    private bool IsAtEnd()
    {
        return Peek().Type == EOF;
    }

    private Token Peek()
    {
        return _tokens[_current];
    }

    private Token Previous()
    {
        return _tokens[_current - 1];
    }

    private static ParseException Error(Token token, string msg)
    {
        Lox.Error(token, msg);
        return new ParseException();
    }

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            if (Previous().Type == SEMICOLON) return;

            switch (Peek().Type)
            {
                case CLASS:
                case FUN:
                case VAR:
                case FOR:
                case IF:
                case WHILE:
                case PRINT:
                case RETURN:
                    return;
            }

            Advance();
        }
    }

    internal enum CallableKind
    {
        FUNCTION,
        METHOD
    }

    internal enum LoopType
    {
        NONE,
        WHILE,
        FOR
    }
}