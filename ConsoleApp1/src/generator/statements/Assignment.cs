using System.Text.Json;
using ConsoleApp1.generator.classes;
using ConsoleApp1.generator.expr;
using ConsoleApp1.generator.print;
using Mono.Cecil.Cil;

namespace ConsoleApp1.generator.statements;

public class Assignment(JsonElement left, JsonElement right, ILProcessor proc)
{
    public static bool IsAssig(JsonElement stmt)
    {
        return stmt.TryGetProperty("L", out _) && stmt.TryGetProperty("R", out _);
    }

    public static Assignment GetAssig(JsonElement stmt, ILProcessor proc)
    {
        return new Assignment(stmt.GetProperty("L"), stmt.GetProperty("R"), proc);
    }
    
    public void Parse()
    {
        string? name = left.GetProperty("Name").GetString();
        string? type;
        
        if (Field.IsField(left))
        {
            JsonElement clsInfo = left.GetProperty("X");
            string? clsName = clsInfo.GetProperty("ExprBase").GetProperty("Typ").GetProperty("TypeName").GetString();
            Field field = Class.Classes[clsName!].Fields[name!];
            string? clsVarName = clsInfo.GetProperty("Name").GetString();
            
            proc.Emit(OpCodes.Ldloc, Statement.Vars[clsVarName!]);
            new Expr(right, false).GenerateExpr(proc);
            proc.Emit(OpCodes.Stfld, field.FieldDefinition);
            
            type = left.GetProperty("ExprBase").GetProperty("Typ").GetProperty("TypeName").GetString();
            Out.GenerateFieldPrint(Statement.Vars[clsVarName!], field.FieldDefinition, type!, proc);
        }
        else
        {
            new Expr(right, false).GenerateExpr(proc);
            proc.Emit(OpCodes.Stloc, Statement.Vars[name!]);
            
            type = left.GetProperty("Typ").GetProperty("Name").GetString();
            Out.GeneratePrint(Statement.Vars[name!], type!, proc);
        }
    }
}