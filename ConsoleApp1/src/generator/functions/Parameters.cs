using System.Text.Json;
using ConsoleApp1.parser;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ConsoleApp1.generator.functions;

// параметры приоритетнее чем глобальные переменные
public class Parameters(JsonElement parameters, MethodDefinition md) 
{
    public static Dictionary<string, int> ParamToIdx = new();
    
    public void GenerateParameters()
    {
        for (int i = 0; i < parameters.GetArrayLength(); i++)
        {
            JsonElement declBase = parameters[i].GetProperty("DeclBase");
            string? name = declBase.GetProperty("Name").GetString();
            ParamToIdx.Add(name!, i);

            string? type = declBase.GetProperty("Typ").GetProperty("TypeName").GetString();
            var param = new ParameterDefinition(name, ParameterAttributes.None, Parser.TypesReferences[type!]);
            md.Parameters.Add(param);
        }
    }
    
    public static void GenerateLdarg(int i, ILProcessor proc)
    {
        if (i == 0)
        {
            proc.Emit(OpCodes.Ldarg_0);
        }
        else if (i == 1)
        {
            proc.Emit(OpCodes.Ldarg_1);
        }
        else if (i == 2)
        {
            proc.Emit(OpCodes.Ldarg_2);
        }
        else if (i == 3)
        {
            proc.Emit(OpCodes.Ldarg_3);
        }
        else
        {
            proc.Emit(OpCodes.Ldarg, i);
        }
    }
}