using ConsoleApp1.parser;

namespace ConsoleApp1;

public class Program 
{  
    public static void Main(string[] args)
    {
        string ast1 = "../../../ast/set2/ast1vector1.json";
        Parser parser = new Parser(ast1);
    }  
}