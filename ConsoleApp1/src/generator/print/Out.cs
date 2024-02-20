using Cecilifier.Runtime;
using ConsoleApp1.parser;
using Mono.Cecil.Cil;

namespace ConsoleApp1.generator.print;

public class Out
{
    private static readonly Dictionary<string, string> Types = new()
    {
        {"Цел64", "System.Int64"},  // types
        {"Строка", "System.String"},
        {"Вещ64", "System.Double"}
    };
    
    public static void GeneratePrint(VariableDefinition varDef, string type, ILProcessor proc)
    {
        var origType = Types[type];
	    
        proc.Emit(OpCodes.Ldloc, varDef);
        proc.Emit(OpCodes.Call, Parser.Asm.MainModule.ImportReference(TypeHelpers.ResolveMethod(
            typeof(System.Console), 
            "WriteLine",
            System.Reflection.BindingFlags.Default|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public, 
            origType)));
    }
}