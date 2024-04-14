﻿using System.Diagnostics;
using System.Text.Json;
using Cecilifier.Runtime;
using ConsoleApp1.generator.expr;
using ConsoleApp1.generator.functions;
using ConsoleApp1.generator.print;
using ConsoleApp1.parser;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ConsoleApp1.generator.statements;

public class Statement(JsonElement stmt)
{
	public static Dictionary<string, VariableDefinition> Vars = new();
	
	public static void GenerateStatements(JsonElement statements, MethodDefinition md, ILProcessor proc)
	{
		for (int i = 0; i < statements.GetArrayLength(); i++)
		{
			Statement statement = new Statement(statements[i]);
			statement.GenerateStatement(null, proc, md);
		}
	}
	
    public void GenerateStatement(TypeReference? returnType, ILProcessor proc, MethodDefinition md)
    {
        if (Declaration.IsDecl(stmt))
        {
	        Declaration.GetDecl(stmt, md, proc).Parse();
            return;
        }
        
        if (UnaryOperator.IsUnary(stmt))
        {
	        UnaryOperator.GetUnary(stmt, proc).Parse();
	        return;
        }
        
        if (stmt.TryGetProperty("L", out JsonElement lAssig) && stmt.TryGetProperty("R", out JsonElement rAssig))
        {
            ParseAssignment(lAssig, rAssig, proc);
            return;
        }
        
        if (stmt.TryGetProperty("Cond", out JsonElement cond) && stmt.TryGetProperty("Then", out JsonElement then) && 
            stmt.TryGetProperty("Else", out JsonElement els))
        {
	        ParseIfElse(cond, then, els, md, proc);
	        return;
        }
        
        if (stmt.TryGetProperty("Cond", out JsonElement wCond) && stmt.TryGetProperty("Seq", out JsonElement seq))
        {
	        ParseWhile(wCond, seq, md, proc);
	        return;
        }
        
        if (stmt.TryGetProperty("X", out JsonElement x) && stmt.TryGetProperty("Cases", out JsonElement cases) && 
            stmt.TryGetProperty("Else", out JsonElement sEls))
        {
	        ParseSwitch(x, cases, sEls, md, proc);
	        return;
        }
        
        if (stmt.TryGetProperty("X", out JsonElement xReturn) && stmt.TryGetProperty("ReturnTyp", out _))
        {
	        ParseReturn(xReturn, proc);
	        return;
        }
        
        if (stmt.TryGetProperty("X", out JsonElement xCall) && xCall.TryGetProperty("Call", out JsonElement call)) // BE CAREFUL HERE BECAUSE EXCEPTION HAS "X" ONLY
        {
	        ParseFunCall(call, proc);
	        return;
        }
        
        // ADD ANYTHING WITH "X" HERE
        
        if (stmt.TryGetProperty("X", out JsonElement ex)) // BE CAREFUL HERE BECAUSE EXCEPTION HAS "X" ONLY
        {
	        ParseException(ex, proc);
	        return;
        }
        
        Print("no");
    }

    public void ParseReturn(JsonElement xReturn, ILProcessor proc)
    {
	    new Expr(xReturn).GenerateExpr(proc);
    }

    private void ParseAssignment(JsonElement l, JsonElement r, ILProcessor proc)
    {
	    string? name = l.GetProperty("Name").GetString();
	    string? type = l.GetProperty("Typ").GetProperty("Name").GetString();

	    new Expr(r).GenerateExpr(proc);
	    proc.Emit(OpCodes.Stloc, Vars[name!]);

	    Out.GeneratePrint(Vars[name!], type!, proc);
    }

    public void ParseFunCall(JsonElement call, ILProcessor proc)
    {
	    string? name = call.GetProperty("Name").GetString();
	    proc.Emit(OpCodes.Call, Function.Funs[name!]);
	    proc.Emit(OpCodes.Pop); // todo pop only if not void and not assignment
    }

    public void ParseException(JsonElement x, ILProcessor proc)
    {
	    Value.GenerateValue(x, proc);
	    proc.Emit(OpCodes.Newobj, Parser.Asm.MainModule.ImportReference(TypeHelpers.ResolveMethod(
		    typeof(System.Exception), 
		    ".ctor",
		    System.Reflection.BindingFlags.Default|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public,
		    "System.String")));
	    
	    proc.Emit(OpCodes.Throw);
    }

    public void ParseSwitch(JsonElement x, JsonElement cases, JsonElement els, MethodDefinition md, ILProcessor proc)
    {
	    // todo if a.	выбор по выражению (§6.8.1)
	    GenerateExpSwitch(x, cases, els, md, proc);

	    // todo if b.	выбор по предикатам (§6.8.2) 
	    // todo if c.	выбор по типу (§6.8.3)
    }

    public void GenerateExpSwitch(JsonElement x, JsonElement cases, JsonElement els, MethodDefinition md,
	    ILProcessor proc)
    {
	    var name = x.GetProperty("Name").GetString();
	    var type = x.GetProperty("Typ").GetProperty("Name").GetString();
	    
	    var switchCondition = new VariableDefinition(Parser.TypesReferences[type!]);
	    md.Body.Variables.Add(switchCondition);
	    proc.Emit(OpCodes.Ldloc, Vars[name!]);
	    proc.Emit(OpCodes.Stloc, switchCondition);
	    
	    var endOfSwitch = proc.Create(OpCodes.Nop);
	    var defaultCase = proc.Create(OpCodes.Nop);
	    
	    // generate instructions
	    Instruction?[] instructions = new Instruction?[cases.GetArrayLength()];
	    for (int i = 0; i < cases.GetArrayLength(); i++)
	    {
		    instructions[i] = proc.Create(OpCodes.Nop);
	    }
	    
	    // generate conditions
	    for (int i = 0; i < cases.GetArrayLength(); i++)
	    {
		    var conds = cases[i].GetProperty("Exprs"); // arr
		    JsonElement cond = conds[0]; // todo check why 0 (can there be more?)
		    
		    proc.Emit(OpCodes.Ldloc, switchCondition);
		    new Expr(cond).GenerateExpr(proc);
		    proc.Emit(OpCodes.Beq_S, instructions[i]);
	    }
	    
	    proc.Emit(OpCodes.Br, defaultCase);
	    
	    // generate sequence
	    for (int i = 0; i < cases.GetArrayLength(); i++)
	    {
		    proc.Append(instructions[i]);
		    var statements = cases[i].GetProperty("Seq").GetProperty("Statements");
		    GenerateStatements(statements, md, proc);
		    proc.Emit(OpCodes.Br, endOfSwitch);
	    }
	    
	    // generate default block
	    proc.Append(defaultCase);
	    var elseStatements = els.GetProperty("Statements");
	    GenerateStatements(elseStatements, md, proc);
	    proc.Emit(OpCodes.Br, endOfSwitch);
	    proc.Append(endOfSwitch);
    }

    public void ParseWhile(JsonElement cond, JsonElement seq, MethodDefinition md, ILProcessor proc)
    {
	    var condDef = new VariableDefinition(Parser.Asm.MainModule.TypeSystem.Boolean);
	    md.Body.Variables.Add(condDef);
	    GenerateCondition(cond, proc, condDef);
	    
	    // int i = 0;
	    var var_i = new VariableDefinition(Parser.Asm.MainModule.TypeSystem.Int32);
	    md.Body.Variables.Add(var_i);
	    proc.Emit(OpCodes.Ldc_I4, 0);
	    proc.Emit(OpCodes.Stloc, var_i);

	    var lblFel = proc.Create(OpCodes.Nop);
	    var nop = proc.Create(OpCodes.Nop);
	    proc.Append(nop);
	    
	    proc.Emit(OpCodes.Ldloc, condDef); // while condDef is not false
	    proc.Emit(OpCodes.Brfalse, lblFel);
	    
	    var statements = seq.GetProperty("Statements"); // arr
	    for (int i = 0; i < statements.GetArrayLength(); i++)
	    {
		    GenerateCondition(cond, proc, condDef);
		    Statement statement = new Statement(statements[i]);
		    statement.GenerateStatement(null, proc, md);
	    }
	    
	    proc.Emit(OpCodes.Ldloc, var_i);
	    proc.Emit(OpCodes.Dup);
	    proc.Emit(OpCodes.Ldc_I4_1);
	    proc.Emit(OpCodes.Add);
	    proc.Emit(OpCodes.Stloc, var_i);
	    proc.Emit(OpCodes.Pop);
	    proc.Emit(OpCodes.Br, nop);
	    proc.Append(lblFel);
    }
    
    public void GenerateCondition(JsonElement cond, ILProcessor proc, VariableDefinition condDef)
    {
	    string? type = cond.GetProperty("Typ").GetProperty("Name").GetString();
	    if (type != null) new Expr(cond).GenerateExpr(proc);
	    proc.Emit(OpCodes.Stloc, condDef);
    }

    // todo check whether elif exists
    public void ParseIfElse(JsonElement cond, JsonElement then, JsonElement? els, MethodDefinition md, ILProcessor proc)
    {
	    new Expr(cond).GenerateExpr(proc);
		
	    var elseEntryPoint = proc.Create(OpCodes.Nop); 
	    proc.Emit(OpCodes.Brfalse, elseEntryPoint);
	    
	    var ifStatements = then.GetProperty("Statements"); // arr
	    GenerateStatements(ifStatements, md, proc);
	    
	    var elseEnd = proc.Create(OpCodes.Nop);

	    if (els.HasValue)
	    {
		    var endOfIf = proc.Create(OpCodes.Br, elseEnd);
		    proc.Append(endOfIf);
		    proc.Append(elseEntryPoint);
		    // else
		    var elsStatements = els.Value.GetProperty("Statements"); // arr
		    GenerateStatements(elsStatements, md, proc);
	    }
	    else
	    {
		    proc.Append(elseEntryPoint);
	    }
	    proc.Append(elseEnd);
	    md.Body.OptimizeMacros();
    }
    
    
    
    private void Print(Object o)
    {
	    Console.WriteLine(o);
    }
}