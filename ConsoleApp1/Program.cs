using ConsoleApp1.parser;

namespace ConsoleApp1;

public class Program 
{  
    public static void Main(string[] args)
    {
        string ast1 = "C:\\Users\\321av\\GolandProjects\\trivilNet\\ast.json";
        Parser parser = new Parser(ast1);
    }  
}