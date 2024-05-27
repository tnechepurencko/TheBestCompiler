using Cecilifier.Runtime;
using ConsoleApp1.parser;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ConsoleApp1.generator.print;

public class Out
{
    public static readonly Dictionary<string, string> Types = new()
    {
        {"Цел64", "System.Int64"},  // types
        {"Строка", "System.String"},
        {"Вещ64", "System.Double"},
        {"Лог", "System.Boolean"},
        {"Байт", "System.Int32"}, // like int
        {"Слово64", "System.UInt64"},
    };
    
    public static void GeneratePrint(VariableDefinition varDef, string type, ILProcessor proc)
    {
        if (type.Equals("Пусто"))
        {
            return;
        }
        
        var origType = Types[type];
	    
        proc.Emit(OpCodes.Ldloc, varDef);
        proc.Emit(OpCodes.Call, Parser.Asm.MainModule.ImportReference(TypeHelpers.ResolveMethod(
            typeof(System.Console), 
            "WriteLine",
            System.Reflection.BindingFlags.Default|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public, 
            origType)));
    }

    public static void GenerateFieldPrint(VariableDefinition clsVd, FieldDefinition fd, string type, ILProcessor proc)
    {
        proc.Emit(OpCodes.Ldloc, clsVd);
        proc.Emit(OpCodes.Ldfld, fd);
        proc.Emit(OpCodes.Call, Parser.Asm.MainModule.ImportReference(TypeHelpers.ResolveMethod(
            typeof(System.Console), 
            "WriteLine",
            System.Reflection.BindingFlags.Default|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public, 
            Types[type])));

    }
}