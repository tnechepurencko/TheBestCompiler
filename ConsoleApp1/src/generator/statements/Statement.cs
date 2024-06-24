using System.Text.Json;
using Cecilifier.Runtime;
using ConsoleApp1.generator.expr;
using ConsoleApp1.generator.functions;
using ConsoleApp1.parser;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ConsoleApp1.generator.statements;

public class Statement(JsonElement stmt)
{
	public static Dictionary<string, VariableDefinition> Vars = new();
	public static List<string> alFileTypes = new() {"css","htm","html","txt","xml"};
	
	public static void GenerateStatements(JsonElement statements, MethodDefinition md, ILProcessor proc)
	{
		for (int i = 0; i < statements.GetArrayLength(); i++)
		{
			Statement statement = new Statement(statements[i]);
			statement.GenerateStatement(proc, md);
		}
	}
	
    public void GenerateStatement(ILProcessor proc, MethodDefinition md)
    {
	    if (Break.IsBreak(stmt))
	    {
		    Break.GenerateBreak(proc);
		    return;
	    }
	    
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
        
        if (Assignment.IsAssig(stmt))
        {
            Assignment.GetAssig(stmt, proc).Parse();
            return;
        }
        
        if (IfElse.IsIfElse(stmt))
        {
	        IfElse.GetIfElse(stmt, md, proc).Parse();
	        return;
        }
        
        if (While.IsWhile(stmt))
        {
	        While.GetWhile(stmt, md, proc).Parse();
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
        
        if (stmt.TryGetProperty("X", out JsonElement xCall) && xCall.TryGetProperty("Call", out _)) // BE CAREFUL HERE BECAUSE EXCEPTION HAS "X" ONLY
        {
	        ParseFunCall(xCall, proc);
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
	    new Expr(xReturn, false).GenerateExpr(proc);
    }

    public void ParseFunCall(JsonElement x, ILProcessor proc)
    {
	    JsonElement call = x.GetProperty("Call");
	    JsonElement args = x.GetProperty("Args"); // arr

	    for (int i = 0; i < args.GetArrayLength(); i++)
	    {
		    Expr expr = new Expr(args[i], false);
		    expr.GenerateExpr(proc);
	    }
		    
	    string? name = call.GetProperty("Name").GetString();
	    proc.Emit(OpCodes.Call, Function.Funs[name!]);
	    proc.Emit(OpCodes.Pop); // todo pop only if not void and not assignment
    }

    public void ParseException(JsonElement x, ILProcessor proc)
    {
	    Value.GenerateValue(x, proc, false);
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
		    new Expr(cond, false).GenerateExpr(proc);
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

    // public void ParseWhile(JsonElement cond, JsonElement seq, MethodDefinition md, ILProcessor proc)
    // {
	   //  var condDef = new VariableDefinition(Parser.Asm.MainModule.TypeSystem.Boolean);
	   //  md.Body.Variables.Add(condDef);
	   //  GenerateCondition(cond, proc, condDef);
	   //  
	   //  // int i = 0;
	   //  var var_i = new VariableDefinition(Parser.Asm.MainModule.TypeSystem.Int32);
	   //  md.Body.Variables.Add(var_i);
	   //  proc.Emit(OpCodes.Ldc_I4, 0);
	   //  proc.Emit(OpCodes.Stloc, var_i);
    //
	   //  var lblFel = proc.Create(OpCodes.Nop);
	   //  var nop = proc.Create(OpCodes.Nop);
	   //  proc.Append(nop);
	   //  
	   //  proc.Emit(OpCodes.Ldloc, condDef); // while condDef is not false
	   //  proc.Emit(OpCodes.Brfalse, lblFel);
	   //  
	   //  var statements = seq.GetProperty("Statements"); // arr
	   //  for (int i = 0; i < statements.GetArrayLength(); i++)
	   //  {
		  //   GenerateCondition(cond, proc, condDef);
		  //   Statement statement = new Statement(statements[i]);
		  //   statement.GenerateStatement(null, proc, md);
	   //  }
	   //  
	   //  proc.Emit(OpCodes.Ldloc, var_i);
	   //  proc.Emit(OpCodes.Dup);
	   //  proc.Emit(OpCodes.Ldc_I4_1);
	   //  proc.Emit(OpCodes.Add);
	   //  proc.Emit(OpCodes.Stloc, var_i);
	   //  proc.Emit(OpCodes.Pop);
	   //  proc.Emit(OpCodes.Br, nop);
	   //  proc.Append(lblFel);
    // }
    
    // public void GenerateCondition(JsonElement cond, ILProcessor proc, VariableDefinition condDef)
    // {
	   //  string? type = cond.GetProperty("Typ").GetProperty("Name").GetString();
	   //  if (type != null) new Expr(cond, false).GenerateExpr(proc);
	   //  proc.Emit(OpCodes.Stloc, condDef);
    // }
    
    private void Print(Object o)
    {
	    Console.WriteLine(o);
    }
}