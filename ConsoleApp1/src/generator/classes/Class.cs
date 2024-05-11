using System.Text.Json;
using Cecilifier.Runtime;
using ConsoleApp1.parser;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ConsoleApp1.generator.classes;

public class Class(JsonElement cls)
{
    private TypeDefinition? _classTypeDefinition;
    private Dictionary<string, Field> _fields = new();
    
    public void GenerateClass()
    {
        string? name = cls.GetProperty("DeclBase").GetProperty("Name").GetString();
        JsonElement fields = cls.GetProperty("DeclBase").GetProperty("Typ").GetProperty("Fields"); // arr
        
        _classTypeDefinition = new TypeDefinition("", name, TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.NotPublic, Parser.Asm.MainModule.TypeSystem.Object);
        Parser.Asm.MainModule.Types.Add(_classTypeDefinition);

        for (int i = 0; i < fields.GetArrayLength(); i++)
        {
            GenerateField(fields[i]);
        }

        GenerateCtor();
    }

    private void GenerateField(JsonElement field)
    {
        Field f = new Field(field);
        _fields.Add(f.Name, f);
        _classTypeDefinition!.Fields.Add(f.FieldDefinition);
    }

    private void GenerateCtor()
    {
        var attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName;
        var md = new MethodDefinition(".ctor", attributes, Parser.Asm.MainModule.TypeSystem.Void);
        _classTypeDefinition!.Methods.Add(md);
        var proc = md.Body.GetILProcessor();

        //public int l = 7;
        foreach (var field in _fields.Values)
        {
            proc.Emit(OpCodes.Ldarg_0);
            field.Value.GenerateExpr(proc);
            proc.Emit(OpCodes.Stfld, field.FieldDefinition);
        }
        
        proc.Emit(OpCodes.Ldarg_0);
        proc.Emit(OpCodes.Call, Parser.Asm.MainModule.ImportReference(TypeHelpers.DefaultCtorFor(_classTypeDefinition.BaseType)));
        proc.Emit(OpCodes.Ret);
    }
}