using System.Text.Json;
using ConsoleApp1.generator.expr;
using ConsoleApp1.parser;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ConsoleApp1.generator.statements;

public class While(JsonElement cond, JsonElement seq, MethodDefinition md, ILProcessor proc)
{
    public static bool IsWhile(JsonElement stmt)
    {
        return stmt.TryGetProperty("Cond", out _) && stmt.TryGetProperty("Seq", out _);
    }

    public static While GetWhile(JsonElement stmt, MethodDefinition md, ILProcessor proc)
    {
        return new While(stmt.GetProperty("Cond"), stmt.GetProperty("Seq"), md, proc);
    }

    public void Parse()
    {
        var condDef = new VariableDefinition(Parser.Asm.MainModule.TypeSystem.Boolean);
        md.Body.Variables.Add(condDef);
        GenerateCondition(condDef);
	    
        // int i = 0;
        var varIter = new VariableDefinition(Parser.Asm.MainModule.TypeSystem.Int32);
        md.Body.Variables.Add(varIter);
        proc.Emit(OpCodes.Ldc_I4, 0);
        proc.Emit(OpCodes.Stloc, varIter);

        var lblFel = proc.Create(OpCodes.Nop);
        var nop = proc.Create(OpCodes.Nop);
        proc.Append(nop);
	    
        proc.Emit(OpCodes.Ldloc, condDef); // while condDef is not false
        proc.Emit(OpCodes.Brfalse, lblFel);
	    
        var statements = seq.GetProperty("Statements"); // arr
        for (int i = 0; i < statements.GetArrayLength(); i++)
        {
            GenerateCondition(condDef);
            Statement statement = new Statement(statements[i]);
            statement.GenerateStatement(null, proc, md);
        }
	    
        proc.Emit(OpCodes.Ldloc, varIter);
        proc.Emit(OpCodes.Dup);
        proc.Emit(OpCodes.Ldc_I4_1);
        proc.Emit(OpCodes.Add);
        proc.Emit(OpCodes.Stloc, varIter);
        proc.Emit(OpCodes.Pop);
        proc.Emit(OpCodes.Br, nop);
        proc.Append(lblFel);
    }
    
    public void GenerateCondition(VariableDefinition condDef)
    {
        string? type = cond.GetProperty("Typ").GetProperty("Name").GetString();
        if (type != null) new Expr(cond, false).GenerateExpr(proc);
        proc.Emit(OpCodes.Stloc, condDef);
    }
}