using System.Text.Json;

namespace ConsoleApp1.parser;

public class Parser
{
    private readonly JsonElement _ast;
    private Generator _generator;
    
    public Parser(string path)
    {
        string data = File.ReadAllText(path);
        JsonDocument doc = JsonDocument.Parse(data);
        JsonElement root = doc.RootElement;
        _ast = root;

        _generator = new Generator();
    }

    public void StartParsing()
    {
        ParseModule(_ast);
    }

    private void ParseModule(JsonElement module)
    {
        var imports = module.GetProperty("Imports"); // arr
        for (int i = 0; i < imports.GetArrayLength(); i++)
        {
            ParseImport(imports[i]);
        }
        
        var decls = module.GetProperty("Decls"); // arr
        for (int i = 0; i < decls.GetArrayLength(); i++)
        {
            ParseDecl(decls[i]);
        }
        
        var statements = module.GetProperty("Entry").GetProperty("Seq").GetProperty("Statements"); // arr
        for (int i = 0; i < statements.GetArrayLength(); i++)
        {
            ParseStatement(statements[i]);
        }
    }

    private void ParseImport(JsonElement import)
    {
        
    }

    private void ParseDecl(JsonElement decl)
    {
        string? name = decl.GetProperty("Name").GetString();
        string? type = decl.GetProperty("Typ").GetProperty("Name").GetString();
        Print(name);
        Print(type);
        
        JsonElement init = decl.GetProperty("Init");
        ParseExpr(init);
    }

    private void ParseExpr(JsonElement expr)
    {
        // todo if field "X"
        // todo if field "Y"
        string? type = expr.GetProperty("Typ").GetProperty("Name").GetString();
        
        if (type!.Equals("Цел64"))
        {
            long value = expr.GetProperty("IntVal").GetInt64();
            _generator.GenerateInt64(value);
        }
    }

    private void ParseStatement(JsonElement stmt)
    {
        JsonElement decl;
        if (stmt.TryGetProperty("D", out decl))
        {
            // declaration processing
            ParseDecl(decl);
            return;
        }
        
        JsonElement lAssig;
        JsonElement rAssig;
        if (stmt.TryGetProperty("L", out lAssig) && stmt.TryGetProperty("R", out rAssig))
        {
            // assignment processing
            ParseAssignment(lAssig, rAssig);
            return;
        }
        
        Print("no");
    }

    private void ParseAssignment(JsonElement l, JsonElement r)
    {
        
    }

    private void Print(Object o)
    {
        Console.WriteLine(o);
    }
}