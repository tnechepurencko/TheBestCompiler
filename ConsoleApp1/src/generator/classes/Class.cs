using System.Text.Json;
using Cecilifier.Runtime;
using ConsoleApp1.generator.expr;
using ConsoleApp1.generator.statements;
using ConsoleApp1.parser;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ConsoleApp1.generator.classes;

public class Class(JsonElement cls)
{
    private MethodDefinition? _ctorMd;
    private TypeDefinition? _classTypeDefinition;
    public Dictionary<string, Field> Fields = new();
    public static Dictionary<string, Class> Classes = new();
    
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
        Classes.Add(name!, this);
    }

    private void GenerateField(JsonElement field)
    {
        Field f = new Field(field);
        Fields.Add(f.Name, f);
        _classTypeDefinition!.Fields.Add(f.FieldDefinition);
    }

    private void GenerateCtor()
    {
        var attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName;
        _ctorMd = new MethodDefinition(".ctor", attributes, Parser.Asm.MainModule.TypeSystem.Void);
        _classTypeDefinition!.Methods.Add(_ctorMd);
        var proc = _ctorMd.Body.GetILProcessor();

        foreach (var field in Fields.Values)
        {
            proc.Emit(OpCodes.Ldarg_0);
            field.Value.GenerateExpr(proc);
            proc.Emit(OpCodes.Stfld, field.FieldDefinition);
        }
        
        proc.Emit(OpCodes.Ldarg_0);
        proc.Emit(OpCodes.Call, Parser.Asm.MainModule.ImportReference(TypeHelpers.DefaultCtorFor(_classTypeDefinition.BaseType)));
        proc.Emit(OpCodes.Ret);
    }

    public static void GenerateClassDecl(JsonElement value, string name, string type, MethodDefinition md, ILProcessor proc)
    {
        Class cls = Classes[type];
        
        var vd = new VariableDefinition(cls._classTypeDefinition);
        Statement.Vars.Add(name, vd);
        
        md.Body.Variables.Add(vd);
        proc.Emit(OpCodes.Newobj, cls._ctorMd);
        proc.Emit(OpCodes.Stloc, vd);

        JsonElement values = value.GetProperty("Values"); // arr
        for (int i = 0; i < values.GetArrayLength(); i++)
        {
            string? fName = values[i].GetProperty("Name").GetString();
            JsonElement newValue = values[i].GetProperty("Value");
            
            Field field = cls.Fields[fName!];
            
            proc.Emit(OpCodes.Ldloc, vd);
            Expr expr = new Expr(newValue, false);
            expr.GenerateExpr(proc);
            proc.Emit(OpCodes.Stfld, field.FieldDefinition);
        }
    }
}