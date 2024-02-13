using System.Diagnostics;
using System.Text.Json;
using Mono.Cecil.Cil;

namespace ConsoleApp1.generator.expr;

public class Value
{
    private static readonly Dictionary<string, OpCode> Types  = new()
    {
        {"Цел64", OpCodes.Ldc_I4}, // todo this is for int32, need to change
        {"Строка", OpCodes.Ldstr} // todo this is for int32, need to change
    };
    
    public static void GenerateValue(JsonElement single, ILProcessor proc)
    {
        var type = single.GetProperty("Typ").GetProperty("Name").GetString();
        Debug.Assert(type != null, nameof(type) + " != null");
        
        if (type.Equals("Цел64"))
        {
            var value = single.GetProperty("IntVal").GetInt32(); // todo not int32
            proc.Emit(Types[type], value);
        } 
        else if (type.Equals("Строка"))
        {
            proc.Emit(Types[type], GetString(single));
        }
    }

    private static string GetString(JsonElement single)
    {
        var chars = single.GetProperty("StrVal"); // arr
        char[] charArray = new char[chars.GetArrayLength()];
        
        for (int i = 0; i < chars.GetArrayLength(); i++)
        {
            charArray[i] = (char)chars[i].GetInt32();
        }
        
        return new string(charArray);
    } 
}