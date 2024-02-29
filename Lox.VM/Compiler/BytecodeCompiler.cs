using System.Diagnostics;

using Generated;

using LoxVM.Chunk;
using LoxVM.Value;

using Shared;
using Shared.ErrorHandling;

namespace LoxVM.Compiler;

internal class BytecodeCompiler : Stmt.IVoidVisitor, Expr.IVoidVisitor
{
    internal bool HadError => _errors.Count > 0;
    internal IEnumerable<CompilerError> Errors => _errors;
    private readonly List<CompilerError> _errors;

    private readonly List<Stmt> _statements;

    private readonly ObjFunction _function;
    private readonly FunctionType _functionType;

    private const ushort MAX_LOCAL_COUNT = byte.MaxValue + 1;
    private readonly Local[] _locals;
    private readonly UpValue[] _upValues;

    private int scopeDepth;
    private int localCount;

    private readonly BytecodeCompiler? _enclosing;

    private ClassCompiler? currentClass;

    private int latestLine; // Remember the last line number we saw, so that we can use it when we want to add an instruction, but do not know the line number. (E.g. when emitting a POP instruction in EndScope())

    private Loop? loop;

    public BytecodeCompiler(List<Stmt> stmts)
    {
        _statements = stmts;
        _errors = [];

        _function = ObjFunction.TopLevel();
        _locals = new Local[MAX_LOCAL_COUNT];
        _locals[localCount++] = new() { Depth = 0, Name = "", IsCaptured = false };
        _upValues = new UpValue[byte.MaxValue];

        _functionType = FunctionType.Script;

        currentClass = null;
        loop = null;
    }

    public BytecodeCompiler(BytecodeCompiler compiler, FunctionType type, int arity, string fnName, ClassCompiler? classCompiler)
    {
        _enclosing = compiler;
        _statements = [];
        _errors = [];

        _functionType = type;
        localCount = 0;
        scopeDepth = 0;

        _locals = new Local[MAX_LOCAL_COUNT];
        _function = Obj.Func(arity, fnName);
        _locals[localCount++] = new() { Depth = 0, Name = type != FunctionType.Function ? "this" : "", IsCaptured = false };
        _upValues = new UpValue[byte.MaxValue];

        currentClass = classCompiler;
        loop = null;
    }

    public ObjFunction Compile()
    {
        // TODO
        foreach (Stmt stmt in _statements)
        {
            EmitBytecode(stmt);
        }

        EmitReturn();
        return _function;
    }

    private ObjFunction CompileFunction(Stmt.Function function)
    {
        BeginScope();
        foreach (Token param in function.Params)
        {
            AddLocal(param);
            MarkInitialized();
        }

        EmitBytecode(function.Body);

        EmitReturn();

        return _function;
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
        foreach (Stmt stmt in stmts)
        {
            EmitBytecode(stmt);
        }
    }

    private void EmitBytecode(IEnumerable<Expr> exprs)
    {
        foreach (Expr expr in exprs)
        {
            EmitBytecode(expr);
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
        if (_functionType == FunctionType.Script)
        {
            AddCompileError("Cannot return from top-level code.", stmt.Keyword);
        }

        if (stmt.Value is null)
        {
            EmitReturn();
        }
        else
        {
            if (_functionType == FunctionType.Initializer)
            {
                AddCompileError("Cannot return a value from an initializer.", stmt.Keyword);
            }

            EmitBytecode(stmt.Value);
            EmitByte(OpCode.Return, stmt.Keyword.Line);
        }
    }

    public void VisitPrintStmt(Stmt.Print stmt)
    {
        EmitBytecode(stmt.Ex);
        EmitByte(OpCode.Print, stmt.Line);
    }

    public void VisitVarStmt(Stmt.Var stmt)
    {
        DeclareVariable(stmt.Name);

        if (stmt.Initializer is not null)
        {
            EmitBytecode(stmt.Initializer);
        }
        else
        {
            EmitByte(OpCode.Nil, stmt.Name.Line);
        }

        DefineVariable(stmt.Name);
    }

    public void VisitBlockStmt(Stmt.Block stmt)
    {
        BeginScope();
        EmitBytecode(stmt.Statements);
        EndScope();
    }

    public void VisitIfStmt(Stmt.If stmt)
    {
        EmitBytecode(stmt.Condition);

        int thenJump = EmitJump(OpCode.JumpIfFalse, GetLineNumber(stmt.Condition));

        EmitByte(OpCode.Pop);
        EmitBytecode(stmt.ThenBranch);

        int elseJump = EmitJump(OpCode.Jump, GetLineNumber(stmt.Condition));

        PatchJump(thenJump);

        EmitByte(OpCode.Pop);
        if (stmt.ElseBranch is not null)
        {
            EmitBytecode(stmt.ElseBranch);
        }
        PatchJump(elseJump);
    }

    public void VisitWhileStmt(Stmt.While stmt)
    {
        Loop? enclosing = loop;
        loop = new() { Enclosing = enclosing, IsForLoop = false };

        int loopStart = _function.Chunk.Count;
        loop.LoopStart = loopStart; // Save the address of the start of the loop. so that if there is a 'continue' statement in the body, we can emit a loop instruction.
        EmitBytecode(stmt.Condition);

        int line = GetLineNumber(stmt.Condition);

        int exitJump = EmitJump(OpCode.JumpIfFalse, line);
        EmitByte(OpCode.Pop, line);

        EmitBytecode(stmt.Body);

        EmitLoop(loopStart, line);

        PatchJump(exitJump);
        EmitByte(OpCode.Pop);

        foreach (int breakJump in loop.BreakLocations)
        {
            PatchJump(breakJump); // Emit the address where the 'break' statement(s) will jump to end the loop.
        }

        loop = loop.Enclosing;
    }

    public void VisitForStmt(Stmt.For stmt)
    {
        BeginScope();

        Loop? enclosing = loop;
        loop = new() { Enclosing = enclosing, IsForLoop = true };

        if (stmt.Initializer is not null)
        {
            EmitBytecode(stmt.Initializer);
        }

        int loopStart = _function.Chunk.Count;

        (int exitJump, int conditionLine) = (-1, -1);
        if (stmt.Condition is not null)
        {
            EmitBytecode(stmt.Condition);

            conditionLine = GetLineNumber(stmt.Condition);

            exitJump = EmitJump(OpCode.JumpIfFalse, conditionLine);
            EmitByte(OpCode.Pop, conditionLine);
        }

        EmitBytecode(stmt.Body);

        foreach (int contLocation in loop.ContinueLocations)
        {
            PatchJump(contLocation); // If the body has any continue statements, they need to jump here, just before the increment is executed.
        }

        if (stmt.Increment is not null)
        {
            EmitBytecode(stmt.Increment);
            EmitByte(OpCode.Pop, GetLineNumber(stmt.Increment));
        }

        EmitLoop(loopStart, stmt.Line);

        if (exitJump != -1)
        {
            PatchJump(exitJump);
            EmitByte(OpCode.Pop, conditionLine);
        }

        foreach (int breakJump in loop.BreakLocations)
        {
            PatchJump(breakJump);
        }

        loop = loop.Enclosing;

        EndScope();
    }

    public void VisitFunctionStmt(Stmt.Function stmt)
    {
        DeclareVariable(stmt.Name);
        MarkInitialized();
        Function(FunctionType.Function, stmt);
        DefineVariable(stmt.Name);
    }

    public void VisitClassStmt(Stmt.Class stmt)
    {
        DeclareVariable(stmt.Name);
        EmitBytes(stmt.IsStatic ? OpCode.StaticClass : OpCode.Class, MakeConstant(LoxValue.Object(Obj.Str(stmt.Name.Lexeme))), stmt.Name.Line);
        DefineVariable(stmt.Name);

        ClassCompiler classCompiler = new() { Enclosing = currentClass, HasSuperClass = false };
        currentClass = classCompiler;

        if (stmt.Superclass is not null)
        {
            if (stmt.Superclass.Name.Lexeme == stmt.Name.Lexeme)
            {
                AddCompileError("A class can't inherit from itself.", stmt.Name);
            }

            NamedVariable(stmt.Superclass.Name);

            BeginScope();
            Token superToken = new(TokenType.IDENTIFIER, "super", null, stmt.Name.Line);
            AddLocal(superToken);
            DefineVariable(superToken);

            NamedVariable(stmt.Name);
            EmitByte(OpCode.Inherit, stmt.Superclass.Name.Line);
            classCompiler.HasSuperClass = true;
        }

        NamedVariable(stmt.Name);

        foreach (Stmt.Function method in stmt.Methods)
        {
            byte constant = MakeConstant(LoxValue.Object(Obj.Str(method.Name.Lexeme)));

            bool isInitializer = method.Name.Lexeme == "init";

            Function(isInitializer ? FunctionType.Initializer : FunctionType.Method, method);

            EmitBytes(OpCode.Method, constant, method.Name.Line);
        }

        EmitByte(OpCode.Pop);

        if (currentClass.HasSuperClass)
        {
            EndScope();
        }

        currentClass = classCompiler.Enclosing;
    }

    public void VisitBreakStmt(Stmt.Break stmt)
    {
        if (loop is null)
        {
            AddCompileError("Cannot use 'break' outside of a loop.", stmt.Keyword);
            return;
        }

        int jumpLocation = EmitJump(OpCode.Jump, stmt.Keyword.Line);
        loop.BreakLocations.Add(jumpLocation);

    }

    public void VisitContinueStmt(Stmt.Continue stmt)
    {
        if (loop is null)
        {
            AddCompileError("Cannot use 'continue' outside of a loop.", stmt.Keyword);
            return;
        }

        // In for loops, the continue statement causes the execution to jump forward, just before the execution of the increment.
        // We are still in the body, so we don't know where exactly that will be.
        if (loop.IsForLoop)
        {
            int jumpLocation = EmitJump(OpCode.Jump, stmt.Keyword.Line);
            loop.ContinueLocations.Add(jumpLocation);
        }
        else
        {
            // In a while loop, we jump back to the beginning of the loop (the address indicated by loop.LoopStart).
            EmitLoop(loop.LoopStart, stmt.Keyword.Line);
        }
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

    public void VisitLogicalExpr(Expr.Logical expr)
    {
        EmitBytecode(expr.Right);
        EmitBytecode(expr.Left);

        int line = expr.Oper.Line;

        switch (expr.Oper.Type)
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
        switch (expr.Operator.Type)
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
        int line = expr.Token is not null ? expr.Token.Line : latestLine;

        switch (expr.Value)
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
                throw new UnreachableException($"{nameof(VisitLiteralExpr)} can not handle {expr.Value.GetType()} values.");
        }
    }

    public void VisitVariableExpr(Expr.Variable expr) => NamedVariable(expr.Name);

    public void VisitAssignExpr(Expr.Assign expr)
    {
        EmitBytecode(expr.Value);
        SetVariable(expr.Name, 2);
    }

    public void VisitCallExpr(Expr.Call expr)
    {
        int count = expr.Arguments.Count;
        if (count > 255)
        {
            AddCompileError("Can't have more than 255 parameters.", expr.Paren);
        }

        if (expr.Callee is Expr.Get getExpr)
        {
            // The code is accessing a method and calling it immediately.
            // So replace the GetProperty and Call instructions with a dedicated Invoke instruction.

            byte argCount = (byte)expr.Arguments.Count;
            byte name = MakeConstant(LoxValue.Object(Obj.Str(getExpr.Name.Lexeme)));
            EmitBytecode(getExpr.Obj);
            EmitBytecode(expr.Arguments);

            EmitBytes(OpCode.Invoke, name, getExpr.Name.Line);
            EmitByte(argCount, getExpr.Name.Line);
        }
        else
        {
            EmitBytecode(expr.Callee);

            EmitBytecode(expr.Arguments);
            EmitBytes(OpCode.Call, (byte)count, expr.Paren.Line);
        }
    }

    public void VisitGetExpr(Expr.Get expr)
    {
        EmitBytecode(expr.Obj);

        byte name = MakeConstant(LoxValue.Object(Obj.Str(expr.Name.Lexeme)));

        EmitBytes(OpCode.GetProperty, name, expr.Name.Line);
    }

    public void VisitSetExpr(Expr.Set expr)
    {
        EmitBytecode(expr.Obj);

        byte name = MakeConstant(LoxValue.Object(Obj.Str(expr.Name.Lexeme)));

        EmitBytecode(expr.Value);
        EmitBytes(OpCode.SetProperty, name, expr.Name.Line);
    }

    public void VisitThisExpr(Expr.This expr)
    {
        if (currentClass is null)
        {
            AddCompileError("Can't use 'this' outside of a class.", expr.Keyword);
            return;
        }

        NamedVariable(expr.Keyword);
    }

    public void VisitSuperExpr(Expr.Super expr)
    {
        if (currentClass is null)
        {
            AddCompileError("Cannot use 'super' outside of a class.", expr.Keyword);
        }
        else if (!currentClass.HasSuperClass)
        {
            AddCompileError("Cannot use 'super' in a class with no superclass.");
        }

        byte name = MakeConstant(LoxValue.Object(Obj.Str(expr.Method.Lexeme)));
        NamedVariable(new Token(TokenType.IDENTIFIER, "this", null, expr.Method.Line));
        NamedVariable(new Token(TokenType.IDENTIFIER, "super", null, expr.Method.Line));
        EmitBytes(OpCode.GetSuper, name, expr.Method.Line);
    }

    public void VisitPostfixExpr(Expr.Postfix expr)
    {
        bool isPlus = expr.Operator.Type == TokenType.PLUS_PLUS;
        int line = expr.Operator.Line;

        OpCode op = isPlus ? OpCode.Add : OpCode.Subtract;

        if (expr.Obj is Expr.Variable varExpr)
        {
            // Load the current value of the variable to the top of the stack. This will be the 'return' value of the postfix expression. 
            NamedVariable(varExpr.Name);

            // Load the constant '1', and the current value (again).
            EmitConstant(LoxValue.Number(1), line);
            NamedVariable(varExpr.Name);

            // Perform the postfix operation. This will leave the result on the top of the stack
            EmitByte(op, line);

            // Set the value of the variable.
            SetVariable(varExpr.Name, line);

            // Remove the result of the expression from the top of the stack. This will leave the original value on the top.
            EmitByte(OpCode.Pop, line);
        }
        else if (expr.Obj is Expr.Get getExpr)
        {
            byte name = MakeConstant(LoxValue.Object(Obj.Str(getExpr.Name.Lexeme)));

            // Compile the target of the property (the class instance)
            EmitBytecode(getExpr.Obj);

            // Load the current value
            EmitBytes(OpCode.GetProperty, name, line);

            // Compile the target again, so that after the postfix operation is complete, it is at the right place on stack for the SetProperty op.
            EmitBytecode(getExpr.Obj);

            // Emit the constant '1', and the current value again.
            EmitConstant(LoxValue.Number(1), line);
            EmitBytecode(getExpr.Obj);
            EmitBytes(OpCode.GetProperty, name, line);

            // Postfix operation
            EmitByte(op, line);

            // Set property
            EmitBytes(OpCode.SetProperty, name, line);

            // Remove the result from the stack.
            EmitByte(OpCode.Pop, line);

        }
        else
        {
            AddCompileError("The postfix operator can only be applied to a variable or a property.", expr.Operator);
        }
    }

    #endregion

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

    #region Utilities

    private void Function(FunctionType type, Stmt.Function function)
    {
        int arity = function.Params.Count;
        string name = function.Name.Lexeme;

        BytecodeCompiler compiler = new(this, type, arity, name, currentClass);

        ObjFunction fun = compiler.CompileFunction(function);
        fun.IsStatic = function.IsStatic;
        _errors.AddRange(compiler._errors);

        EmitBytes(OpCode.Closure, MakeConstant(LoxValue.Object(fun)), latestLine);

        foreach (UpValue upValue in compiler._upValues.Take(fun.UpValueCount))
        {
            EmitByte((byte)(upValue.IsLocal ? 1 : 0));
            EmitByte(upValue.Index);
        }
    }

    #region Variable Declaration

    #region Jumping And Looping

    private void PatchJump(int offset)
    {
        int jump = _function.Chunk.Count - offset - 2;

        if (jump > ushort.MaxValue)
        {
            AddCompileError("Too much code to jump over.");
        }

        _function.Chunk[offset] = (byte)((jump >> 8) & 0xFF);
        _function.Chunk[offset + 1] = (byte)(jump & 0xFF);
    }

    private int EmitJump(OpCode instruction, int line)
    {
        EmitByte(instruction, line);
        EmitByte(0xFF, line);
        EmitByte(0xFF, line);
        return _function.Chunk.Count - 2;
    }

    private void EmitLoop(int loopStart, int line)
    {
        EmitByte(OpCode.Loop, line);

        int offset = _function.Chunk.Count - loopStart + 2;

        if (offset > ushort.MaxValue)
        {
            AddCompileError("Loop body too large");
        }

        EmitByte((byte)((offset >> 8) & 0xFF), line);
        EmitByte((byte)(offset & 0xFF), line);
    }

    #endregion

    private void BeginScope() => scopeDepth++;

    private void EndScope()
    {
        scopeDepth--;

        while (localCount > 0 && _locals[localCount - 1].Depth > scopeDepth)
        {
            if (_locals[localCount - 1].IsCaptured)
            {
                EmitByte(OpCode.CloseUpValue);
            }
            else
            {
                EmitByte(OpCode.Pop);

            }
            localCount--;
        }
    }

    private void SetVariable(Token variableName, int line)
    {
        int arg = ResolveLocal(variableName);

        if (arg != -1)
        {
            EmitBytes(OpCode.SetLocal, (byte)arg, line);
        }
        else if ((arg = ResolveUpValue(variableName)) != -1)
        {
            EmitBytes(OpCode.SetUpValue, (byte)arg, line);
        }
        else
        {
            AssignGlobal(variableName);
        }
    }

    private void NamedVariable(Token name)
    {
        int arg = ResolveLocal(name);

        if (arg != -1)
        {
            EmitBytes(OpCode.GetLocal, (byte)arg, name.Line);
        }
        else if ((arg = ResolveUpValue(name)) != -1)
        {
            EmitBytes(OpCode.GetUpValue, (byte)arg, name.Line);
        }
        else
        {
            ReadGlobal(name);
        }
    }


    private void DefineVariable(Token name)
    {
        if (scopeDepth > 0)
        {
            MarkInitialized();
            return;
        }

        DefineGlobal(name);
    }


    private void MarkInitialized()
    {
        if (scopeDepth == 0)
        {
            return;
        }
        _locals[localCount - 1].Depth = scopeDepth;
    }

    private void DefineGlobal(Token name)
    {
        byte global = MakeConstant(LoxValue.Object(name.Lexeme));

        EmitBytes(OpCode.DefineGlobal, global, name.Line);
    }


    private void DeclareVariable(Token name)
    {
        if (scopeDepth == 0)
        {
            return;
        }

        AddLocal(name);
    }

    private void AddLocal(Token name)
    {
        if (localCount == MAX_LOCAL_COUNT)
        {
            AddCompileError("Too many local variables in function.", name);
            return;
        }

        for (int i = localCount - 1; i >= 0; i--)
        {
            Local l = _locals[i];

            if (l.Depth != -1 && l.Depth < scopeDepth)
            {
                break;
            }

            if (l.Name == name.Lexeme)
            {
                AddCompileError("Already a variable with this name in this scope.", name);
            }
        }

        Local local = new() { Name = name.Lexeme, Depth = -1, IsCaptured = false };
        _locals[localCount++] = local;
    }

    private int ResolveLocal(Token name)
    {
        for (int i = localCount - 1; i >= 0; i--)
        {
            Local local = _locals[i];

            if (name.Lexeme == local.Name)
            {
                if (local.Depth == -1)
                {
                    AddCompileError("Can't read local variable in its own initializer.", name);
                }
                return i;
            }
        }

        return -1;
    }

    private void ReadGlobal(Token name)
    {
        byte arg = MakeConstant(LoxValue.Object(name.Lexeme));
        EmitBytes(OpCode.GetGlobal, arg, name.Line);
    }

    private void AssignGlobal(Token token)
    {
        byte arg = MakeConstant(LoxValue.Object(token.Lexeme));

        EmitBytes(OpCode.SetGlobal, arg, token.Line);
    }

    private int ResolveUpValue(Token name)
    {
        if (_enclosing is null)
        {
            return -1;
        }

        int local = _enclosing.ResolveLocal(name);
        if (local != -1)
        {
            _enclosing._locals[local].IsCaptured = true;
            return AddUpValue((byte)local, true, name);
        }

        int upValue = _enclosing.ResolveUpValue(name);
        if (upValue != -1)
        {
            return AddUpValue((byte)upValue, false, name);
        }

        return -1;
    }

    private int AddUpValue(byte index, bool isLocal, Token name)
    {
        int upValueCount = _function.UpValueCount;

        for (int i = 0; i < upValueCount; i++)
        {
            UpValue upValue = _upValues[i];
            if (upValue.Index == index && upValue.IsLocal == isLocal)
            {
                return i;
            }
        }

        if (upValueCount == byte.MaxValue)
        {
            AddCompileError("Too many closure variables in function.", name);
            return 0;
        }

        UpValue upVal = new() { Index = index, IsLocal = isLocal };
        _upValues[upValueCount] = upVal;
        return _function.UpValueCount++;
    }


    #endregion

    private void EmitByte(OpCode opCode) => EmitByte(opCode, latestLine);

    private void EmitByte(OpCode opCode, int line)
    {
        _function.Chunk.WriteChunk(opCode, line);
        latestLine = line;
    }


    private void EmitByte(byte val, int line)
    {
        _function.Chunk.WriteChunk(val, line);
        latestLine = line;
    }

    private void EmitByte(byte val) => _function.Chunk.WriteChunk(val, latestLine);

    private void EmitBytes(OpCode opCode, byte val) => EmitBytes(opCode, val, latestLine);
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
        int constant = _function.Chunk.AddConstant(value);
        if (constant > byte.MaxValue)
        {
            AddCompileError("Too many constants in one chunk.");
            return 0;
        }

        return (byte)constant;
    }

    private void EmitConstant(LoxValue value, int line)
    {
        byte constant = MakeConstant(value);
        _function.Chunk.WriteChunk(OpCode.Constant, line);
        _function.Chunk.WriteChunk(constant, line);
    }

    private void EmitReturn()
    {
        if (_functionType == FunctionType.Initializer)
        {
            EmitBytes(OpCode.GetLocal, 0);
        }
        else
        {
            EmitByte(OpCode.Nil);
        }

        EmitByte(OpCode.Return);

    }

    private int GetLineNumber(Expr expr) // Try to get the line number of an Expr by checking the actual type. It would be better to save the line number in the Expr and Stmt records, but that would require many changes in the parser.
    {
        if (expr is Expr.Grouping grouping)
        {
            return GetLineNumber(grouping);
        }
        else if (expr is Expr.Logical logical)
        {
            return logical.Oper.Line;
        }
        else if (expr is Expr.Binary binary)
        {
            return binary.Operator.Line;
        }
        else if (expr is Expr.Literal literal)
        {
            return literal.Token!.Line;
        }
        else if (expr is Expr.Assign assign)
        {
            return assign.Name.Line;
        }
        else if (expr is Expr.Postfix postfix)
        {
            return postfix.Operator.Line;
        }
        else if (expr is Expr.Variable variable)
        {
            return variable.Name.Line;
        }

        throw new NotImplementedException($"{nameof(GetLineNumber)} is not implemented for {expr.GetType()}.");
    }

    private void AddCompileError(string msg, Token? token = null) => _errors.Add(new(msg, token));

    #endregion
}

internal class Local
{
    internal required string Name { get; init; }
    internal int Depth { get; set; }
    internal bool IsCaptured { get; set; }
}

internal class ClassCompiler
{
    internal ClassCompiler? Enclosing { get; set; }
    internal bool HasSuperClass { get; set; }
}

internal readonly struct UpValue
{
    internal byte Index { get; init; }
    internal bool IsLocal { get; init; }
}

/// <summary>
/// Used to keep track of loop state, to allow compiling 'break' and 'continue' statements.
/// </summary>
internal class Loop
{
    /// <summary>
    /// A reference to the enclosing loop. If there are no enclosing loops, it is null.
    /// </summary>
    internal Loop? Enclosing { get; set; }

    /// <summary>
    /// A list that keeps track of the addresses of the jump locations for break statements in the current loop.
    /// Used for backpatching.
    /// </summary>
    internal List<int> BreakLocations { get; } = [];

    /// <summary>
    /// The address of the start of the current loop. 
    /// Used to emit a Loop instruction for the continue statement in while loops.
    /// </summary>
    internal int LoopStart { get; set; }

    /// <summary>
    /// True if the surrounding loop is a for loop.
    /// </summary>
    internal bool IsForLoop { get; set; }

    /// <summary>
    /// A list that keeps track of the addresses if the jump locations for continue statements in for loops.
    /// </summary>
    internal List<int> ContinueLocations { get; } = [];
}

internal enum FunctionType
{
    Function,
    Script,
    Method,
    Initializer
}
