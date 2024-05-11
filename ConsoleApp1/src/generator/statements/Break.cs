using System.Text.Json;
using Mono.Cecil.Cil;

namespace ConsoleApp1.generator.statements;

public class Break
{
    public static bool IsBreak(JsonElement stmt)
    {
        return stmt.TryGetProperty("Break", out _);
    }

    public static void GenerateBreak(ILProcessor proc)
    {
        proc.Emit(OpCodes.Br, While.WhileStack.Peek());
    }
}