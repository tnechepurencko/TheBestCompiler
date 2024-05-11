using System.Text.Json;
using Cecilifier.Runtime;
using ConsoleApp1.generator.classes;
using ConsoleApp1.generator.expr;
using ConsoleApp1.generator.functions;
using ConsoleApp1.generator.print;
using ConsoleApp1.parser;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ConsoleApp1.generator.statements;

public class Declaration(JsonElement decl, MethodDefinition md, ILProcessor proc)
{
    public static bool IsDecl(JsonElement stmt)
    {
        return stmt.TryGetProperty("D", out _);
    }
    
    public static Declaration GetDecl(JsonElement stmt, MethodDefinition md, ILProcessor proc)
    {
        return new Declaration(stmt.GetProperty("D"), md, proc);
    }

    public void Parse()
    {
        GenerateVarDecl(); 
    }
    
    public void GenerateVarDecl()
    {  
	    JsonElement descBase = decl.GetProperty("DeclBase");
	    string? name = descBase.GetProperty("Name").GetString();
	    JsonElement value = decl.GetProperty("Init");
	    bool isFun = value.TryGetProperty("Call", out _);
	    
	    if (isFun)
	    {
		    GenerateFunDecl(descBase, value, name!);
		    return;
	    }

	    string type = GetType(descBase);
	    if (Parser.TypesReferences.ContainsKey(type)) // fun or simple var
	    {
		    GenerateVariableDecl(value, name!, type);
	    }
	    else if (Vector.Vectors.ContainsKey(type)) // vector
	    {
		    Vector vector = new Vector(value, name!, type, md, proc);
		    vector.GenerateVector();
	    }
	    else if (Class.Classes.ContainsKey(type))
	    {
		    Class.GenerateClassDecl(value, name!, type, md, proc);
	    }
    }

    private string GetType(JsonElement declBase)
    {
	    if (declBase.GetProperty("Typ").TryGetProperty("Name", out JsonElement name))
	    {
		    return name.GetString()!;
	    }

	    return declBase.GetProperty("Typ").GetProperty("TypeName").GetString()!;
    }

    private void GenerateVariableDecl(JsonElement value, string name, string type)
    {
	    var vd = new VariableDefinition(Parser.TypesReferences[type]);
	    Statement.Vars.Add(name, vd);
	    md.Body.Variables.Add(vd);

	    if (type.Equals("Пусто"))
	    {
		    proc.Emit(OpCodes.Initobj, Parser.TypesReferences[type]);
	    }
	    else
	    {
		    new Expr(value, false).GenerateExpr(proc);
		    proc.Emit(OpCodes.Stloc, vd);
	    }
	    
	    Out.GeneratePrint(vd, type, proc);
    }
    
    private void GenerateFunDecl(JsonElement declBase, JsonElement value, string name)
    {
	    string? type = declBase.GetProperty("Typ").GetProperty("TypeName").GetString();
	    
	    var vd = new VariableDefinition(Parser.TypesReferences[type!]);
	    Statement.Vars.Add(name, vd);
	    md.Body.Variables.Add(vd);

	    JsonElement args = value.GetProperty("Args"); // arr
		    
	    for (int i = 0; i < args.GetArrayLength(); i++)
	    {
		    Expr expr = new Expr(args[i], false);
		    expr.GenerateExpr(proc);
	    }
		    
	    string? funName = value.GetProperty("Call").GetProperty("Name").GetString();
	    proc.Emit(OpCodes.Call, Function.Funs[funName!]);
	    proc.Emit(OpCodes.Stloc, vd);
	    
	    Out.GeneratePrint(vd, type!, proc);
    }
}