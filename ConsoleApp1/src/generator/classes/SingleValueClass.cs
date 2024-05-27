using System.Text.Json;
using Cecilifier.Runtime;
using ConsoleApp1.generator.expr;
using ConsoleApp1.parser;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ConsoleApp1.generator.classes;

public class SingleValueClass(JsonElement constant)
{
    public static Dictionary<string, FieldDefinition> Constants = new();
    
    public void GenerateClass()
    {
        string? name = constant.GetProperty("DeclBase").GetProperty("Name").GetString();
        string? type = constant.GetProperty("DeclBase").GetProperty("Typ").GetProperty("TypeName").GetString();
        JsonElement value = constant.GetProperty("Value");
        
        //Class : NewCls
        var clsTypeDef = new TypeDefinition("", name, TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.NotPublic, Parser.Asm.MainModule.TypeSystem.Object);
        Parser.Asm.MainModule.Types.Add(clsTypeDef);

        var constField = new FieldDefinition("constField", FieldAttributes.Public | FieldAttributes.Static, Parser.TypesReferences[type!]);
        clsTypeDef.Fields.Add(constField);
        Constants.Add(name!, constField);

        //** Constructor: NewCls() **
        var ctorMd = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName, Parser.Asm.MainModule.TypeSystem.Void);
        clsTypeDef.Methods.Add(ctorMd);
        var ctorProc = ctorMd.Body.GetILProcessor();
        ctorProc.Emit(OpCodes.Ldarg_0);
        ctorProc.Emit(OpCodes.Call, Parser.Asm.MainModule.ImportReference(TypeHelpers.DefaultCtorFor(clsTypeDef.BaseType)));
        ctorProc.Emit(OpCodes.Ret);

        //** Constructor: NewCls() **
        var cctorMd = new MethodDefinition(".cctor", MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName, Parser.Asm.MainModule.TypeSystem.Void);
        clsTypeDef.Methods.Add(cctorMd);
        var cctorProc = cctorMd.Body.GetILProcessor();

        // Generate const value
        Expr expr = new Expr(value, false);
        expr.GenerateExpr(cctorProc);
        cctorProc.Emit(OpCodes.Stsfld, constField);
        cctorProc.Emit(OpCodes.Ret);
    }
}