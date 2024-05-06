using System.Diagnostics;
using System.Text.Json;
using ConsoleApp1.generator.functions;
using ConsoleApp1.generator.statements;
using ConsoleApp1.parser;
using Mono.Cecil.Cil;

namespace ConsoleApp1.generator.expr;

public class Expr(JsonElement operation, bool isIndex)
{
    private readonly Dictionary<int, List<OpCode>> _operators = new()
    {
        { 11, [OpCodes.Add] }, // + 11
        { 12, [OpCodes.Sub] }, // - 12
        { 13, [OpCodes.Mul] }, // * 13
        { 14, [OpCodes.Div] }, // / 14
        { 15, [OpCodes.Rem] }, // % 15
        { 27, [OpCodes.Ceq] }, // = 27
        { 28, [OpCodes.Clt] }, // < 28
        { 29, [OpCodes.Cgt] }, // > 29
        { 30, [OpCodes.Ceq, OpCodes.Ldc_I4_0, OpCodes.Ceq] }, // # 30
        { 31, [OpCodes.Cgt, OpCodes.Ldc_I4_0, OpCodes.Ceq] }, // <= 31
        { 32, [OpCodes.Clt, OpCodes.Ldc_I4_0, OpCodes.Ceq] } // >= 32
    };

    private bool IsSingle()
    {
        return !(operation.TryGetProperty("X", out _) && operation.TryGetProperty("Y", out _) &&
                operation.TryGetProperty("Op", out _));
    }

    private bool IsVar()
    {
        return operation.TryGetProperty("Name", out _) && operation.TryGetProperty("Obj", out _);
    }

    private Expr GetLeft()
    {
        return new Expr(operation.GetProperty("X"), isIndex);
    }
    
    private Expr GetRight()
    {
        return new Expr(operation.GetProperty("Y"), isIndex);
    }

    private int GetOperator()
    {
        return operation.GetProperty("Op").GetInt32();
    }

    private void GenerateSingleValue(ILProcessor proc)
    {
        if (IsVar())
        {
            var name = operation.GetProperty("Name").GetString();

            if (Parameters.ParamToIdx.ContainsKey(name!)) // parameter
            {
                int paramIdx = Parameters.ParamToIdx[name!];
                Parameters.GenerateLdarg(paramIdx, proc);
            }
            else // main/global var
            {
                proc.Emit(OpCodes.Ldloc, Statement.Vars[name!]);
            }
            return;
        }
        
        Value.GenerateValue(operation, proc, isIndex);
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