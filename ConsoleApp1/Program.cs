using ConsoleApp1.parser;

namespace ConsoleApp1;

public class Program 
{  
    public static void Main(string[] args)
    {
        // string ast1 = "../../../ast/ast13true.json";
        string ast1 = "../../../ast/ast14null.json";
        // string ast1 = "../../../ast/ast3assig.json";
        Parser parser = new Parser(ast1);
    }  
}