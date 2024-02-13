using System.Diagnostics;
using System.Text.Json;
using Mono.Cecil.Cil;

namespace ConsoleApp1.generator.expr;

public class Expr(JsonElement operation)
{
    private readonly Dictionary<string, OpCode> _types  = new()
    {
        {"Цел64", OpCodes.Ldc_I4} // todo this is for int32, need to change
    };

    private readonly Dictionary<int, List<OpCode>> _operators = new()
    {
        { 11, new List<OpCode> { OpCodes.Add} }, // + 11
        { 12, new List<OpCode> { OpCodes.Sub} }, // - 12
        { 13, new List<OpCode> { OpCodes.Mul} }, // * 13
        { 14, new List<OpCode> { OpCodes.Div} }, // / 14
        { 27, new List<OpCode> { OpCodes.Ceq} }, // = 27
        { 28, new List<OpCode> { OpCodes.Clt} }, // < 28
        { 29, new List<OpCode> { OpCodes.Cgt} }, // > 29
        { 31, new List<OpCode> { OpCodes.Cgt, OpCodes.Ldc_I4_0, OpCodes.Ceq } }, // <= 31
        { 32, new List<OpCode> { OpCodes.Clt, OpCodes.Ldc_I4_0, OpCodes.Ceq } } // >= 32
    };

    private bool IsSingle()
    {
        return !(operation.TryGetProperty("X", out _) && operation.TryGetProperty("Y", out _) &&
                operation.TryGetProperty("Op", out _));
    }

    private Expr GetLeft()
    {
        return new Expr(operation.GetProperty("X"));
    }
    
    private Expr GetRight()
    {
        return new Expr(operation.GetProperty("Y"));
    }

    private int GetOperator()
    {
        return operation.GetProperty("Op").GetInt32();
    }

    private void GenerateSingleValue(ILProcessor proc)
    {
        var type = operation.GetProperty("Typ").GetProperty("Name").GetString();
        Debug.Assert(type != null, nameof(type) + " != null");
        
        if (type.Equals("Цел64"))
        {
            var value = operation.GetProperty("IntVal").GetInt32(); // todo not int32
            proc.Emit(_types[type], value);
        }
    }

    private void GenerateOperator(ILProcessor proc)
    {
        foreach (var op in _operators[GetOperator()])
        {
            proc.Emit(op);
        }
    }

    public void GenerateExpr(ILProcessor proc)
    {
        if (IsSingle())
        {
            GenerateSingleValue(proc);
            return;
        }

        GetLeft().GenerateExpr(proc);
        GetRight().GenerateExpr(proc);
        GenerateOperator(proc);
    }
}