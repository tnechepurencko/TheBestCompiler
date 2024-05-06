using System.Text.Json;
using ConsoleApp1.generator.expr;
using ConsoleApp1.generator.print;
using Mono.Cecil.Cil;

namespace ConsoleApp1.generator.statements;

public class Assignment(JsonElement left, JsonElement right, ILProcessor proc)
{
    public static bool IsAssig(JsonElement stmt)
    {
        return stmt.TryGetProperty("L", out JsonElement lAssig) && stmt.TryGetProperty("R", out JsonElement rAssig);
    }

    public static Assignment GetAssig(JsonElement stmt, ILProcessor proc)
    {
        return new Assignment(stmt.GetProperty("L"), stmt.GetProperty("R"), proc);
    }
    
    public void Parse()
    {
        string? name = left.GetProperty("Name").GetString();
        string? type = left.GetProperty("Typ").GetProperty("Name").GetString();

        new Expr(right, false).GenerateExpr(proc);
        proc.Emit(OpCodes.Stloc, Statement.Vars[name!]);

        Out.GeneratePrint(Statement.Vars[name!], type!, proc);
    }
}