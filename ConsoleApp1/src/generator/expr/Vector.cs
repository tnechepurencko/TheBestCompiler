using System.Text.Json;
using Cecilifier.Runtime;
using ConsoleApp1.generator.print;
using ConsoleApp1.generator.statements;
using ConsoleApp1.parser;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ConsoleApp1.generator.expr;

public class Vector(JsonElement vector, string name, string type, MethodDefinition md, ILProcessor proc)
{
    public static Dictionary<string, string> Vectors = new();

    public static Dictionary<string, Type> TypeToType = new()
    {
        { "Цел64", typeof(System.Collections.Generic.List<System.Int64>) },
        { "Строка", typeof(System.Collections.Generic.List<System.String>) },
        { "Вещ64", typeof(System.Collections.Generic.List<System.Double>) },
        { "Лог", typeof(System.Collections.Generic.List<System.Boolean>) },
        { "Слово64", typeof(System.Collections.Generic.List<System.UInt64>) },
        { "Байт", typeof(System.Collections.Generic.List<System.Byte>) },
        // todo do i need "пусто"?
    };

    public static void GenerateVectorType(JsonElement vectorType)
    {
        JsonElement declBase = vectorType.GetProperty("DeclBase");
        string? name = declBase.GetProperty("Name").GetString();
        string? type = declBase.GetProperty("Typ").GetProperty("ElementTyp").GetProperty("TypeName").GetString();
        
        Vectors.Add(name!, type!);
    }
    
    public void GenerateVector()
    {
        string vecType = Vectors[type];
	    
        var vd = new VariableDefinition(Parser.Asm.MainModule.ImportReference(typeof(System.Collections.Generic.List<>)).MakeGenericInstanceType(Parser.TypesReferences[vecType]));
        md.Body.Variables.Add(vd);
        Statement.Vars.Add(name, vd);
	    
        proc.Emit(OpCodes.Newobj, Parser.Asm.MainModule.ImportReference(TypeHelpers.ResolveMethod(TypeToType[vecType], ".ctor",System.Reflection.BindingFlags.Default|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public)));

        JsonElement values = vector.GetProperty("Composite").GetProperty("Values");
        if (values.GetArrayLength() > 0)
        {
            GenerateValues(values, vecType);
        }
        else
        {
            GenerateValuesByDefault(vecType);
        }
        
        proc.Emit(OpCodes.Stloc, vd);
    }

    private void GenerateValuesByDefault(string vecType)
    {
        JsonElement defaultElement = vector.GetProperty("Composite").GetProperty("Default");
        if (defaultElement.ValueKind != JsonValueKind.Null)
        {
            int len = vector.GetProperty("Composite").GetProperty("LenExpr").GetProperty("IntVal").GetInt32();
            for (int i = 0; i < len; i++)
            {
                GenerateValue(defaultElement, vecType);
            }
        }
    }

    private void GenerateValues(JsonElement values, string vecType)
    {
        for (int i = 0; i < values.GetArrayLength(); i++)
        {
            GenerateValue(values[i], vecType);
        }
    }

    private void GenerateValue(JsonElement value, string vecType)
    {
        proc.Emit(OpCodes.Dup);
        Expr expr = new Expr(value);
        expr.GenerateExpr(proc);
        proc.Emit(OpCodes.Callvirt, Parser.Asm.MainModule.ImportReference(TypeHelpers.ResolveMethod(TypeToType[vecType], "Add",System.Reflection.BindingFlags.Default|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public, Out.Types[vecType])));
    }
}