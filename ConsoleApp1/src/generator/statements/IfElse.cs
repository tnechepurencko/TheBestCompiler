using System.Text.Json;
using ConsoleApp1.generator.expr;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ConsoleApp1.generator.statements;

public class IfElse(JsonElement cond, JsonElement then, JsonElement? els, MethodDefinition md, ILProcessor proc)
{
    public static bool IsIfElse(JsonElement stmt)
    {
        return stmt.TryGetProperty("Cond", out _) &&
               (stmt.TryGetProperty("Then", out _) || stmt.TryGetProperty("Else", out _));
    }

    public static IfElse GetIfElse(JsonElement stmt, MethodDefinition methodDef, ILProcessor ilProc)
    {
        stmt.TryGetProperty("Cond", out JsonElement condition);
        stmt.TryGetProperty("Then", out JsonElement thenStmt);
        stmt.TryGetProperty("Else", out JsonElement elsStmt);

        return new IfElse(condition, thenStmt, elsStmt, methodDef, ilProc);
    }

    public void Parse() // todo check whether elif exists
    {
        new Expr(cond, false).GenerateExpr(proc);
		
        var elseEntryPoint = proc.Create(OpCodes.Nop); 
        proc.Emit(OpCodes.Brfalse, elseEntryPoint);

        if (then.ValueKind != JsonValueKind.Undefined)
        {
            var ifStatements = then.GetProperty("Statements"); // arr
            Statement.GenerateStatements(ifStatements, md, proc);
        }
	    
        var elseEnd = proc.Create(OpCodes.Nop);

        if (els.HasValue)
        {
            var endOfIf = proc.Create(OpCodes.Br, elseEnd);
            proc.Append(endOfIf);
            proc.Append(elseEntryPoint);
            // else
            if (_hasElif())
            {
                GetIfElse(els.Value, md, proc).Parse();
            }
            else
            {
                var elsStatements = els.Value.GetProperty("Statements"); // arr
                Statement.GenerateStatements(elsStatements, md, proc);
            }
        }
        else
        {
            proc.Append(elseEntryPoint);
        }
        proc.Append(elseEnd);
        md.Body.OptimizeMacros();
    }

    private bool _hasElif()
    {
        return els.Value.TryGetProperty("Cond", out _);
    }
}