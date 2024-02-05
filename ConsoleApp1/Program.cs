using ConsoleApp1.parser;

namespace ConsoleApp1;

public class Program 
{  
    public static void Main(string[] args)
    {
        string ast1 = "../../../ast/ast1.json";
        Parser parser = new Parser(ast1);
        parser.StartParsing();
    }  
}