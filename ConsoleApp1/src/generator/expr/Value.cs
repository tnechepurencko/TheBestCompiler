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
        {"Байт", OpCodes.Ldc_I4}, // like int
        {"Вещ64", OpCodes.Ldc_R8},
        {"Лог", OpCodes.Ldc_I4}, // like int
        {"Слово64", OpCodes.Ldc_I8}, // like long
    };
    
    public static void GenerateValue(JsonElement single, ILProcessor proc, bool isIndex)
    {
        JsonElement exprBase = single.GetProperty("ExprBase");
        var type = exprBase.GetProperty("Typ").GetProperty("Name").GetString();
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
            if (isIndex)
            {
                proc.Emit(OpCodes.Ldc_I4, single.GetProperty("IntVal").GetInt32());
                return;
            }
            
            var value = single.GetProperty("IntVal").GetInt64();
            proc.Emit(Types[type], value);
        } 
        else if (type.Equals("Лог"))
        {
            var value = single.GetProperty("Obj").GetProperty("Value").GetProperty("Value").GetBoolean();
            // var value = single.GetProperty("IntVal").GetInt64(); // todo imp
            proc.Emit(Types[type], value ? 1 : 0);
        } 
        else if (type.Equals("Пусто"))
        {
            var value = single.GetProperty("IntVal").GetInt64(); // todo change
            proc.Emit(Types[type], value);
        }  
        else if (type.Equals("Байт")) // 0-255
        {
            var value = single.GetProperty("IntVal").GetInt32(); // need check
            proc.Emit(Types[type], value);
        } 
        else if (type.Equals("Слово64")) // uint64
        {
            var value = single.GetProperty("IntVal").GetInt64(); // todo change
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