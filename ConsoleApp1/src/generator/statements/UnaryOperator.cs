using System.Diagnostics;
using System.Text.Json;
using ConsoleApp1.generator.print;
using Mono.Cecil.Cil;

namespace ConsoleApp1.generator.statements;

public class UnaryOperator(JsonElement unary, bool inc, ILProcessor proc)
{
    public static bool IsUnary(JsonElement stmt)
    {
        return stmt.TryGetProperty("LInc", out _) || stmt.TryGetProperty("LDec", out _);
    }

    public static UnaryOperator GetUnary(JsonElement stmt, ILProcessor proc)
    {
        if (stmt.TryGetProperty("LInc", out JsonElement lInc))
        {
            return new UnaryOperator(lInc, true, proc);
        }
        
        return new UnaryOperator(stmt.GetProperty("LDec"), false, proc);
    }
    
    public void Parse()
    {
        var exprBase = unary.GetProperty("ExprBase");
        var type = exprBase.GetProperty("Typ").GetProperty("TypeName").GetString();
        Debug.Assert(type != null, nameof(type) + " != null");
	    
        var name = unary.GetProperty("Name").GetString();
        proc.Emit(OpCodes.Ldloc, Statement.Vars[name!]);
        proc.Emit(OpCodes.Dup);
        proc.Emit(OpCodes.Ldc_I4_1);
	    
        if (inc)
        {
            proc.Emit(OpCodes.Add);
        }
        else
        {
            proc.Emit(OpCodes.Sub);
        }
	    
        proc.Emit(OpCodes.Stloc, Statement.Vars[name!]);
        proc.Emit(OpCodes.Pop);
	    
        Out.GeneratePrint(Statement.Vars[name!], type, proc);
    }
}