using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Mono.Cecil.Cil;

namespace ConsoleApp1.generator.expr;

public class Value
{
    private static readonly Dictionary<string, OpCode> Types  = new()
    {
        {"Цел64", OpCodes.Ldc_I8}, // types
        {"Строка", OpCodes.Ldstr},
        {"Вещ64", OpCodes.Ldc_R8},
        {"Лог", OpCodes.Ldc_I4}
    };
    
    public static void GenerateValue(JsonElement single, ILProcessor proc)
    {
        var type = single.GetProperty("Typ").GetProperty("Name").GetString();
        Debug.Assert(type != null, nameof(type) + " != null");
        
        
        if (type.Equals("Строка")) // types
        {
            proc.Emit(Types[type], GetString(single));
        }
        else if (type.Equals("Вещ64"))
        {
            var floatStr = single.GetProperty("FloatStr").GetString();
            proc.Emit(Types[type], double.Parse(floatStr!, CultureInfo.InvariantCulture));
        }
        else if (type.Equals("Цел64"))
        {
            var value = single.GetProperty("IntVal").GetInt64();
            proc.Emit(Types[type], value);
        } 
        else if (type.Equals("Лог"))
        {
            var value = single.GetProperty("IntVal").GetInt64(); // todo imp
            proc.Emit(Types[type], value);
        } 
        else if (type.Equals("Пусто"))
        {
            var value = single.GetProperty("IntVal").GetInt64(); // todo imp
            proc.Emit(Types[type], value);
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