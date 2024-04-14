using System.Text.Json;
using ConsoleApp1.parser;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ConsoleApp1.generator.functions;

public class Function(JsonElement fun, Parser parser)
{
	public static Dictionary<string, MethodDefinition> Funs = new();
	
    public void GenerateFunction() // todo here is void only
    {
        string? name = fun.GetProperty("DeclBase").GetProperty("Name").GetString();
        JsonElement type = fun.GetProperty("DeclBase").GetProperty("Typ");
        JsonElement? returnType = type.GetProperty("ReturnTyp");
        TypeReference funTypeRef;
        if (returnType.Value.ValueKind == JsonValueKind.Null)
        {
	        funTypeRef = Parser.Asm.MainModule.TypeSystem.Void;
        }
        else
        {
	        string? typeName = returnType.Value.GetProperty("TypeName").GetString();
	        funTypeRef = Parser.TypesReferences[typeName!];
        }
	    
        // generate fun decl
        var funMd = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, funTypeRef);
        parser.MainClassTypeDef.Methods.Add(funMd);
        funMd.Body.InitLocals = true;
        var funProc = funMd.Body.GetILProcessor();
	    
        Funs.Add(name!, funMd);
	    
        JsonElement p = type.GetProperty("Params"); // [] or null
        Parameters parameters = new Parameters(p);
        parameters.GenerateParameters(); // todo
	    
        JsonElement statements = fun.GetProperty("Seq").GetProperty("Statements");
        // generate stmts
        parser.GenerateStatements(statements, funMd, funProc);
	    
        funProc.Emit(OpCodes.Ret);
    }
}