program     -> declaration* EOF ;

declaration -> classDecl | funDecl | statement | varDecl ;

statement   -> exprStmt | forStmt | ifStmt | printStmt | returnStmt | breakStmt | contStmt | continueStmt | whileStmt | block ;

forStmt     -> "for" "(" (varDecl | exprStmt | ";") expression? ";" expression? ";" ")" statement ;
whileStmt   -> "while" "(" expression ")" statement ;
ifStmt      -> "if" "(" expression ")" statement ("else" statement )? ;
block       -> "{" declaration* "}" ;
exprStmt    -> expression ";" ;
printStmt   -> "print" expression ";" ;
returnStmt  -> "return" expression? ";" ;
breakStmt   -> "break" ";" ;
contStmt    -> "continue" ";" ;

classDecl   -> "class" IDENTIFIER ("<" IDENTIFIER)? "{" function* "}" ;
funDecl     -> "fun" function;
function    -> IDENTIFIER "(" parameters? ")" block ;
parameters  -> IDENTIFIER ( "," IDENTIFIER )* ;
varDecl     -> "var" IDENTIFIER ( "=" expression ); ";" ;


expression  -> assignment ;
assignment  -> (call ".")? IDENTIFIER "=" assignment | logicOr ;
logicOr     -> logicAnd ( "or" logicAnd)* ;
logicAnd    -> equality ( "and" equality )* ;
equality    -> comparison ( ( "!=" | "=="   )  comparison )* ;
comparison  -> term ( ( ">" | ">=" | "<" | "<=") term )* ;
term        -> factor ( ( "-" | "+") factor )* ;
factor      -> unary ( ( "/" | "*" |  "%" ) unary )* ;
unary       -> ( "!" | "-" ) unary | call ;
call        -> primary ( "(" arguments? ")" | "." IDENTIFIER )* ;
arguments   -> expression ( "," expression )* ;
primary     -> NUMBER | STRING | "true" | "false" | "nil" | "(" expression ")" | IDENTIFIER | "super" "." IDENTIFIER ;
