using System.Diagnostics;
using Generated;
using static Lox.TokenType;

namespace Lox.Parser;

internal class Parser
{
    private readonly List<Token> _tokens;
    private int _current = 0;

    private int _loopDepth = 0; // Keep track of loop depth, so we can report an error if the code tries to use 'break' or 'continue' outside of a loop
    private bool _inStaticMethod = false; // Keep track if we are in a static method, so we can report an error, if the code tries to use 'this' inside of a static method.  

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

    #region Declarations

    private Stmt Declaration()
    {
        try
        {
            if(Match(STATIC, CLASS))
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
        bool isStatic = Previous().Type == STATIC;

        if(isStatic)
        {
            Consume(CLASS, "Expect 'class' keyword.");
        }

        Token name = Consume(IDENTIFIER, "Expect class name.");

        Expr.Variable? superclass = ParseSuperclass(isStatic);

        Consume(LEFT_BRACE, "Expect '{' before class body.");

        List<Stmt.Function> methods = ParseMethods(isStatic);

        Consume(RIGHT_BRACE, "Expect '}' after class body.");

        return new Stmt.Class(name, superclass, methods, isStatic);
    }

    /// <summary>
    /// Parse the method declarations in the class that is currently being parsed.
    /// </summary>
    /// <param name="isStatic">Flag to indicate if the currently parsed class is marked as static.</param>
    /// <returns>A (possibly empty) list of <see cref="Stmt.Function"/>s.</returns>
    private List<Stmt.Function> ParseMethods(bool isStatic)
    {
        List<Stmt.Function> methods = [];

        while(!Check(RIGHT_BRACE) && !IsAtEnd())
        {
            Stmt.Function method = Function(CallableKind.METHOD);

            if(isStatic && !method.IsStatic)
            {
                Error(method.Name, "A static class may only contain static methods.");
            }

            if(isStatic && method.Name.Lexeme == "init")
            {
                Error(method.Name, "A static class may not contain an initializer.");
            }

            methods.Add(method);
        }

        return methods;
    }

    /// <summary>
    /// Check if the current class declaration has a superclass. If yes, parse the identifier and return it in a <see cref="Expr.Variable"/>
    /// </summary>
    /// <param name="isStatic">Flag to indicate if the currently parsed class is marked as static.</param>
    /// <returns>An <see cref="Expr.Variable"> containing the name of the superclass, if there is one, or <see langword="null"/>.</returns>
    private Expr.Variable? ParseSuperclass(bool isStatic)
    {
        Expr.Variable? superclass = null;
        if(Match(LESS))
        {
            Token superclassName = Consume(IDENTIFIER, "Expect superclass name.");
            if(isStatic)
            {
                Error(superclassName, "A static class may not inherit from another class.");
            }
            superclass = new Expr.Variable(Previous());
        }

        return superclass;
    }

    private Stmt.Function Function(CallableKind kind)
    {
        string kindStr = kind.ToString().ToLowerInvariant();

        bool isStatic = false;

        if(Check(STATIC))
        {
            isStatic = true;
            _inStaticMethod = true;
            Advance(); // Consume 'static' keyword.
        }

        Token name = Consume(IDENTIFIER, $"Expect {kindStr} name.");

        Consume(LEFT_PAREN, $"Expect '(' after {kindStr} name.");
        List<Token> parameters = ParseParameters();

        Consume(RIGHT_PAREN, "Expect ')' after parameters");

        Consume(LEFT_BRACE, $"Expect '{{' before {kindStr} body");
        List<Stmt> body = Block();

        try
        {
            return new Stmt.Function(name, parameters, body, isStatic);
        }
        finally
        {
            _inStaticMethod = false;
        }
    }

    /// <summary>
    /// Parse the parameter list of the function or method that is currently being parsed.
    /// </summary>
    /// <returns>A (possibly empty) list of tokens, one for each parameter.</returns>
    private List<Token> ParseParameters()
    {
        List<Token> parameters = [];
        if(!Check(RIGHT_PAREN))
        {
            do
            {
                if(parameters.Count > 255)
                {
                    Error(Peek(), "Can't have more than 255 parameters");
                }

                parameters.Add(Consume(IDENTIFIER, "Expect parameter name"));
            } while(Match(COMMA));
        }

        return parameters;
    }

    private Stmt.Var VarDeclaration()
    {
        Token name = Consume(IDENTIFIER, "Expect variable name");
        Expr? initializer = null;

        if(Match(EQUAL))
        {
            initializer = Expression();
        }

        Consume(SEMICOLON, "Expect ';' after variable declaration");
        return new Stmt.Var(name, initializer);
    }

    #endregion

    #region Statements

    private Stmt Statement()
    {
        if(Match(FOR))
        {
            return ForStatement();
        }
        if(Match(FOREACH))
        {
            return ForeachStatement();
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

    private Stmt.For ForStatement()
    {
        Consume(LEFT_PAREN, "Expect '(' after 'for'");

        Stmt initializer;
        if(Match(SEMICOLON))
        {
            initializer = null!;
        }
        else if(Match(VAR))
        {
            initializer = VarDeclaration();
        }
        else
        {
            initializer = ExpressionStatement();
        }

        Expr condition = null!;
        if(!Check(SEMICOLON))
        {
            condition = Expression();
        }
        Consume(SEMICOLON, "Expect ';' after loop condition.");

        Expr increment = null!;
        if(!Check(RIGHT_PAREN))
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


    private Stmt.For ForeachStatement()
    {
        Token paren = Consume(LEFT_PAREN, "Expect '(' after 'foreach'.");

        Token name = Consume(IDENTIFIER, "Expect identifier.");

        Consume(IN, "Expect 'in' after name");

        Expr collectionExpr = Expression();

        Consume(RIGHT_PAREN, "Expect ')'.");
        Consume(LEFT_BRACE, "Expect '{' before foreach body.");

        string loopName = $"@loop_{_loopDepth}";
        (Stmt.Var initializer, Expr.Binary condition, Expr.Postfix increment) = DesugarForeach(paren, collectionExpr, loopName);

        try
        {
            _loopDepth++;

            List<Stmt> body = Block();

            // Get the current element from the loop
            // The name of the element is the name given in the foreach loop ('foreach(var name...)')
            Stmt.Var access = new(new Token(name.Type, name.Lexeme, name.Literal, name.Line + 1), new Expr.ArrayAccess(CopyCollectionExpr(collectionExpr, paren.Line, paren), null!, new Expr.Variable(new Token(IDENTIFIER, loopName, null, paren.Line + 1))));

            // Create a new block, with the array access as the first statement.
            Stmt newBody = new Stmt.Block([access, .. body]);

            var tmp = new Stmt.For(initializer, condition, increment, newBody);
            return tmp;
        }
        finally
        {
            _loopDepth--;
        }
    }


    /// <summary>
    /// Desugar foreach loop to a for loop.
    /// Example:
    /// var array = [1, 2, 3];
    /// 
    /// foreach(var num in array) {
    ///     print a;
    /// }
    /// 
    /// // The foreach loop above will be desugared to this:
    /// for(var @loop = 0; @loop < len(array), @loop++) {
    ///     var num = array[@loop];
    ///     {
    ///         print a;
    ///     }
    /// }
    /// </summary>
    /// <param name="paren"></param>
    /// <param name="collectionExpr"></param>
    /// <param name="loopName"></param>
    /// <returns></returns>
    private (Stmt.Var, Expr.Binary, Expr.Postfix) DesugarForeach(Token paren, Expr collectionExpr, string loopName)
    {
        // Create a new variable, '@loop', that will be initialized in the for statement ('for(var @loop = 0; ...)')
        // "@loop" is not a valid user-defined identifier name, so we can be sure that it will not crash with existing identifiers
        // We need to create a new token every time we need to create or access the '@loop' variable, just as if it was parsed from source code.

        // Initialize @loop to 0
        Stmt.Var initializer = new(new(IDENTIFIER, loopName, null, paren.Line), new Expr.Literal(0d));

        // Create a new binary operation, which will be the loop condition
        Expr.Variable var = new(new(IDENTIFIER, loopName, null, paren.Line)); // Same '@loop' variable
        Token oper = new(LESS, "<", null, paren.Line); // Less-than operator
        Expr.Call right = new(new Expr.Variable(new Token(IDENTIFIER, "len", null, paren.Line)), paren, [CopyCollectionExpr(collectionExpr, paren.Line, paren)]); // A call to 'len', a built-in function that gets the length of an array or a string 
        Expr.Binary condition = new(var, oper, right);

        // Increment '@loop'
        Expr.Postfix increment = new(new Expr.Variable(new(IDENTIFIER, loopName, null, paren.Line)), new Token(PLUS_PLUS, "++", null, paren.Line));

        return (initializer, condition, increment);
    }

    // TODO
    /// <summary>
    /// Must create a copy of the collection expression, because when we are resolving names, we need a unique instance.
    /// Since record types have value equality, it is not enough to just create a new Expr, we need to change at least one value
    /// We can, for example, change the Token that identifies the expression or a part of the expression (Depending on the concrete type of the expression).
    /// Possible expression types are:
    /// <see cref="Expr.Variable"/> - A variable can refer to an array or a string
    /// <see cref="Expr.Literal"/> - A literal can be a string
    /// <see cref="Expr.Array"/> - An array
    /// <see cref="Expr.Call"/> - A function or method call can return a string or array
    /// <see cref="Expr.ArrayAccess"/> - Arrays can have string or array members
    /// <see cref="Expr.This"/> - Can refer to an array or string
    /// <see cref="Expr.Get"/> - Can return an array or string 
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="line"></param>
    /// <param name="pos">A token that is used when reporting an error.</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static Expr CopyCollectionExpr(Expr collection, int line, Token pos)
    {
        if(collection is Expr.Variable var)
        {
            return var with { Name = new(var.Name.Type, var.Name.Lexeme, var.Name.Literal, line) };
        }
        if(collection is Expr.Literal lit)
        {
            if(lit.Value is not string)
            {
                throw Error(pos, "'foreach' loop can not be used with a number");
            }
            return lit;
        }
        if(collection is Expr.Array arr)
        {
            return arr with { Bracket = new Token(arr.Bracket.Type, arr.Bracket.Lexeme, arr.Bracket.Literal, line) };
        }
        if(collection is Expr.Call call)
        {
            return call with { Paren = new Token(call.Paren.Type, call.Paren.Lexeme, call.Paren.Literal, line) };
        }
        if(collection is Expr.ArrayAccess arrAcc)
        {
            return arrAcc with { Bracket = new Token(arrAcc.Bracket.Type, arrAcc.Bracket.Lexeme, arrAcc.Bracket.Literal, line) };
        }
        if(collection is Expr.This thisExpr)
        {
            return thisExpr with { Keyword = new Token(thisExpr.Keyword.Type, thisExpr.Keyword.Lexeme, thisExpr.Keyword.Literal, line) };
        }
        if(collection is Expr.Get get)
        {
            return get with { Obj = CopyCollectionExpr(get.Obj, line, pos) };
        }


        throw Error(pos, "Invalid Expression type.");
    }

    private Stmt.Continue ContinueStatement()
    {
        Token keyword = Previous();

        if(_loopDepth == 0)
        {
            throw Error(keyword, "Must be inside a loop to use 'continue'");
        }

        Consume(SEMICOLON, "Expect ';' after 'continue'.");

        return new Stmt.Continue(keyword);

    }

    private Stmt.Break BreakStatement()
    {
        Token keyword = Previous();

        if(_loopDepth == 0)
        {
            throw Error(keyword, "Must be inside a loop to use 'break'");
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

    private Stmt.If IfStatement()
    {
        Consume(LEFT_PAREN, "Expect '(' after 'if'");
        Expr condition = Expression();
        Consume(RIGHT_PAREN, "Expect ')' after condition");

        Stmt thenBranch = Statement();
        Stmt elseBranch = null!;

        if(Match(ELSE))
        {
            elseBranch = Statement();
        }

        return new Stmt.If(condition, thenBranch, elseBranch);
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

    private List<Stmt> Block()
    {
        List<Stmt> statements = [];

        while(!Check(RIGHT_BRACE) && !IsAtEnd())
        {
            statements.Add(Declaration());
        }

        Consume(RIGHT_BRACE, "Expect '}' after block");
        return statements;
    }

    #endregion

    #region Expressions

    private Expr Expression()
    {
        return Assignment();
    }

    private Expr Assignment()
    {
        Expr expr = Or();

        if(Match(EQUAL, PLUS_EQUAL, MINUS_EQUAL, STAR_EQUAL, SLASH_EQUAL, MODULO_EQUAL))
        {
            Token equals = Previous();
            Expr right = Assignment();

            Token oper = GetNewToken(equals);

            Expr value = equals.Type == EQUAL ? right : new Expr.Binary(expr, oper, right);

            if(expr is Expr.Variable var)
            {
                Token name = var.Name;
                return new Expr.Assign(name, value);
            }
            else if(expr is Expr.Get getExpr)
            {
                return new Expr.Set(getExpr.Obj, getExpr.Name, value);
            }
            else if(expr is Expr.ArrayAccess arrayAccessExpr)
            {
                return new Expr.ArrayAssign(arrayAccessExpr.Target, arrayAccessExpr.Bracket, arrayAccessExpr.Index, value);
            }

            Error(equals, "Invalid assignment target");
        }

        return expr;
    }

    /// <summary>
    /// Create a new token based on the type of <paramref name="equals"/>. 
    /// Used to transform compound assignment operator tokens into their non-compound form (e.g. '+=' to '+'), so that a new <see cref="Expr.Binary"/> node can be constructed.
    /// </summary>
    /// <param name="equals">The original token</param>
    /// <returns>A new token, with one of the following <see cref="TokenType"/>s: <see cref="PLUS"/>, <see cref="MINUS"/>, <see cref="STAR"/>, <see cref="SLASH"/>, <see cref="MODULO"/>.</returns>
    /// <exception cref="UnreachableException"></exception>
    private static Token GetNewToken(Token equals)
    {
        // Copy the lexeme, and line from 'equals' into a new token
        Token CreateNewToken(TokenType type)
        {
            // Take the first character from the lexeme, so that we don't have a binary expression with a token that has a lexeme like "+="
            string newLexeme = equals.Lexeme[0].ToString();
            return new(type, newLexeme, equals.Literal, equals.Line);
        }

        Token oper = equals.Type switch
        {
            EQUAL => equals, // Not a compound operator, just assignment
            PLUS_EQUAL => CreateNewToken(PLUS),
            MINUS_EQUAL => CreateNewToken(MINUS),
            STAR_EQUAL => CreateNewToken(STAR),
            SLASH_EQUAL => CreateNewToken(SLASH),
            MODULO_EQUAL => CreateNewToken(MODULO),
            _ => throw new UnreachableException()
        };
        return oper;
    }

    private Expr Or()
    {
        Expr expr = And();

        while(Match(OR))
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

        while(Match(AND))
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

        while(Match(BANG_EQUAL, EQUAL_EQUAL))
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

        while(Match(GREATER, GREATER_EQUAL, LESS, LESS_EQUAL))
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

        while(Match(MINUS, PLUS))
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

        while(Match(SLASH, STAR, MODULO))
        {
            Token oper = Previous();
            Expr right = Unary();
            expr = new Expr.Binary(expr, oper, right);
        }

        return expr;
    }

    private Expr Unary()
    {
        if(Match(MINUS, BANG))
        {
            Token oper = Previous();
            Expr expr = Unary();
            return new Expr.Unary(oper, expr);
        }

        return Call();
    }

    private Expr Call()
    {
        Expr expr = Primary();

        while(true)
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
            else if(Match(LEFT_SQUARE))
            {
                Token bracket = Previous();
                expr = new Expr.ArrayAccess(expr, bracket, Expression());
                Consume(RIGHT_SQUARE, "Expect closing ']'.");
            }
            else
            {
                break;
            }
        }

        if(Match(PLUS_PLUS, MINUS_MINUS)) // Check for postfix expression.
        {
            expr = new Expr.Postfix(expr, Previous());
        }

        return expr;
    }

    private Expr.Call FinishCall(Expr callee)
    {
        List<Expr> arguments = [];

        if(!Check(RIGHT_PAREN))
        {
            do
            {
                if(arguments.Count > 255)
                {
                    throw Error(Peek(), "Can't have more than 255 arguments.");
                }
                arguments.Add(Expression());
            } while(Match(COMMA));
        }

        Token paren = Consume(RIGHT_PAREN, "Expect ')' after arguments.");

        if(Match(PLUS_PLUS, MINUS_MINUS))
        {
            Error(Previous(), "Cannot apply postfix expression to function or method call.");
        }

        return new Expr.Call(callee, paren, arguments);
    }

    private Expr Primary()
    {
        if(Match(FALSE, TRUE, NIL, NUMBER, STRING)) // Check literals
        {
            return LiteralExpression();
        }
        else if(Match(SUPER))
        {
            return SuperExpression();
        }
        else if(Match(LEFT_PAREN))
        {
            return GroupingExpression();
        }
        else if(Match(LEFT_SQUARE))
        {
            return ArrayCreation();
        }
        else if(Match(THIS))
        {
            if(_inStaticMethod)
            {
                Error(Previous(), "Cannot access 'this' in static method.");
            }
            return new Expr.This(Previous());
        }
        else if(Match(IDENTIFIER))
        {
            return new Expr.Variable(Previous());
        }
        else
        {
            throw Error(Peek(), "Expected expression");
        }
    }

    private Expr.Grouping GroupingExpression()
    {
        Expr innerExpr = Expression();
        Consume(RIGHT_PAREN, "Expected ')' after expression.");
        return new Expr.Grouping(innerExpr);
    }

    private Expr.Super SuperExpression()
    {
        Token keyword = Previous();
        Consume(DOT, "Expect '.' after 'super'.");
        Token method = Consume(IDENTIFIER, "Expect superclass method name.");

        return new Expr.Super(keyword, method);
    }

    private Expr.Literal LiteralExpression()
    {
        Token literalToken = Previous();

        return literalToken.Type switch
        {
            TRUE => new(true),
            FALSE => new(false),
            NIL => new(null),
            NUMBER or STRING => new(literalToken.Literal),
            _ => throw new UnreachableException($"{literalToken.Type}: {literalToken.Lexeme}")
        };
    }

    private Expr.Array ArrayCreation()
    {
        Token leftSquare = Previous();

        if(!Check(RIGHT_SQUARE) && !IsAtEnd())
        {
            // If the next token is a semicolon ";", this expr will be the length of the array (e.g. ["a"; 10] -> array with ten 'a's)
            // If the next token is a comma ",", this expression is the first value in the list of array value initializers (e.g. [10,11,12] -> three element array, with elements 10, 11, and 12)
            Expr first = Expression();

            if(Match(SEMICOLON))
            {
                Expr defaultValueCount = Expression();
                Consume(RIGHT_SQUARE, "Expect closing ']'.");
                return new Expr.Array(leftSquare, null, first, defaultValueCount);
            }
            else
            {
                List<Expr> initializers = [first];
                while(Match(COMMA) && !IsAtEnd())
                {
                    initializers.Add(Expression());
                }

                Consume(RIGHT_SQUARE, "Expect closing ']'.");
                return new Expr.Array(leftSquare, initializers, null, null);
            }

        }

        Consume(RIGHT_SQUARE, "Expect closing ']'.");
        return new Expr.Array(leftSquare, [], null, null);
    }

    #endregion

    #region Utility 

    private Token Consume(TokenType type, string msg)
    {
        if(Check(type))
        {
            return Advance();
        }

        throw Error(Peek(), msg);
    }

    private bool Match(params TokenType[] types)
    {
        foreach(TokenType type in types)
        {
            if(Check(type))
            {
                Advance();
                return true;
            }
        }

        return false;
    }

    private Token Advance()
    {
        if(!IsAtEnd())
        {
            _current++;
        }
        return Previous();
    }

    private bool Check(TokenType type)
    {
        if(IsAtEnd())
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

        while(!IsAtEnd())
        {
            if(Previous().Type == SEMICOLON) return;

            switch(Peek().Type)
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

    #endregion
}