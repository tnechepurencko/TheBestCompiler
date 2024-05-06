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
        
        if (HasDefault())
        {
            GenerateValuesByDefault(vecType);
            proc.Emit(OpCodes.Stloc, vd);
            
            if (values.GetArrayLength() > 0)
            {
                GenerateValuesWithIndices(values, vecType);
            }
        }
        else
        {
            GenerateValues(values, vecType);
            proc.Emit(OpCodes.Stloc, vd);
        }
    }

    private void GenerateValuesWithIndices(JsonElement values, string vecType)
    {
        JsonElement indices = vector.GetProperty("Composite").GetProperty("Indexes");
        for (int i = 0; i < indices.GetArrayLength(); i++)
        {
            proc.Emit(OpCodes.Ldloc, Statement.Vars[name]);
            
            Expr exprIdx = new Expr(indices[i], true);
            exprIdx.GenerateExpr(proc);
            
            Expr exprVal = new Expr(values[i], false);
            exprVal.GenerateExpr(proc);
            
            var bindingFlags = System.Reflection.BindingFlags.Default|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public;
            var method = TypeHelpers.ResolveMethod(TypeToType[vecType], "set_Item", bindingFlags, "System.Int32", Out.Types[vecType]);
            proc.Emit(OpCodes.Callvirt, Parser.Asm.MainModule.ImportReference(method));
        }
    }

    private bool HasDefault()
    {
        JsonElement defaultElement = vector.GetProperty("Composite").GetProperty("Default");
        return defaultElement.ValueKind != JsonValueKind.Null;
    }

    private void GenerateValuesByDefault(string vecType)
    {
        JsonElement defaultElement = vector.GetProperty("Composite").GetProperty("Default");
        int len = GetLen();
        for (int i = 0; i < len; i++)
        {
            GenerateValue(defaultElement, vecType);
        }
    }

    private int GetLen() 
    {
        JsonElement lenExpr = vector.GetProperty("Composite").GetProperty("LenExpr");
        if (lenExpr.ValueKind == JsonValueKind.Null)
        {
            int maxIdx = -1;
            JsonElement indices = vector.GetProperty("Composite").GetProperty("Indexes");

            for (int i = 0; i < indices.GetArrayLength(); i++)
            {
                // todo now indices are compared as a single value expressions
                // but here should be a calculator for cases where JsonElem of the index
                // is equal to the math expr

                if (indices[i].GetProperty("IntVal").GetInt32() > maxIdx)
                {
                    maxIdx = indices[i].GetProperty("IntVal").GetInt32();
                }
            }

            return maxIdx + 1;
        }
        
        return lenExpr.GetProperty("IntVal").GetInt32();
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
        Expr expr = new Expr(value, false);
        expr.GenerateExpr(proc);
        
        var bindingFlags = System.Reflection.BindingFlags.Default|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public;
        var method = TypeHelpers.ResolveMethod(TypeToType[vecType], "Add", bindingFlags, Out.Types[vecType]);
        proc.Emit(OpCodes.Callvirt, Parser.Asm.MainModule.ImportReference(method));
    }
}