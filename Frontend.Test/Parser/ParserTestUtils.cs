using Shared;

namespace Frontend.Test.Parser;

public class ParserTestUtils
{
    public static IEnumerable<Token> Scan(string source) 
    {
        Frontend.Scanner.Scanner scanner = new(source);
        
        return scanner.ScanTokens();
    }
}