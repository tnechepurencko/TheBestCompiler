using System.Text.Json;
using ConsoleApp1.generator.expr;
using ConsoleApp1.parser;
using Mono.Cecil;

namespace ConsoleApp1.generator.classes;

public class Field
{
    public Expr Value;
    public string Name;
    public string Type;
    public FieldDefinition FieldDefinition;

    public Field(JsonElement field)
    {
        Name = field.GetProperty("DeclBase").GetProperty("Name").GetString()!;
        Type = field.GetProperty("DeclBase").GetProperty("Typ").GetProperty("TypeName").GetString()!;
        Value = new Expr(field.GetProperty("Init"), false);
        FieldDefinition = new FieldDefinition(Name, FieldAttributes.Public, Parser.TypesReferences[Type]);
    }
}