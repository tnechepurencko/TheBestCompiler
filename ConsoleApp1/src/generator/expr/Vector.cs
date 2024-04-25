using System.Text.Json;
using ConsoleApp1.parser;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace ConsoleApp1.generator.expr;

public class Vector(JsonElement vector)
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
    
    public void GenerateVector()
    {
        JsonElement declBase = vector.GetProperty("DeclBase");
        string? name = declBase.GetProperty("Name").GetString();
        string? type = declBase.GetProperty("Typ").GetProperty("ElementTyp").GetProperty("TypeName").GetString();
        
        Vectors.Add(name!, type!);
    }
}