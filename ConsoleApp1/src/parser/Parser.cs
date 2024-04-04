using System.Diagnostics;
using System.Text.Json;
using Cecilifier.Runtime;
using ConsoleApp1.generator.expr;
using ConsoleApp1.generator.print;

namespace ConsoleApp1.parser;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

public class Parser	
{
    private readonly JsonElement _ast;
    
    public static AssemblyDefinition Asm;
    private TypeDefinition _typeDef;
    private MethodDefinition _mainModule;
    private ILProcessor _mainProc;
    
    private const string _path = "../../../exe/code.exe";
    
    private MethodDefinition _mainRoutineModule;
    
    private Dictionary<string, MethodDefinition> _funs;
    private Dictionary<string, ILProcessor> _funsProcs;
    // private Dictionary<string, Type> _varsTypes;
    public static Dictionary<string, VariableDefinition> Vars = new();
    private Dictionary<string, Tuple<int, ParameterDefinition, Type>> _paramsDefinitions;

    private static Dictionary<string, TypeReference> TypesReferences;
    
    public Parser(string path)
    {
        string data = File.ReadAllText(path);
        JsonDocument doc = JsonDocument.Parse(data);
        JsonElement root = doc.RootElement;
        _ast = root;

        InitDs();

        var mp = new ModuleParameters { Architecture = TargetArchitecture.AMD64, Kind =  ModuleKind.Console, ReflectionImporterProvider = new SystemPrivateCoreLibFixerReflectionProvider() };
        var and = new AssemblyNameDefinition("Program", Version.Parse("1.0.0.0"));
        Asm = AssemblyDefinition.CreateAssembly(and, Path.GetFileName(_path), mp);

        TypesReferences = new() // types
        {
	        { "Цел64", Asm.MainModule.TypeSystem.Int64 },
	        { "Строка", Asm.MainModule.TypeSystem.String },
	        { "Вещ64", Asm.MainModule.TypeSystem.Double },
	        { "Пусто", Asm.MainModule.ImportReference(typeof(System.Nullable<>)).MakeGenericInstanceType(Asm.MainModule.TypeSystem.Int32) },
	        { "Лог", Asm.MainModule.TypeSystem.Boolean },
	        { "Слово64", Asm.MainModule.TypeSystem.UInt64 },
	        { "Байт", Asm.MainModule.TypeSystem.Byte },
        };
	    
        _typeDef = new TypeDefinition("", "Program", TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Public, Asm.MainModule.TypeSystem.Object);
        Asm.MainModule.Types.Add(_typeDef);
        
        _mainRoutineModule = new MethodDefinition("mainRoutineModule", MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig, Asm.MainModule.TypeSystem.Void);
        GenerateMainModule(_mainRoutineModule, _ast);
        
        _mainModule = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, Asm.MainModule.TypeSystem.Void);
        _typeDef.Methods.Add(_mainModule);
        _mainModule.Body.InitLocals = true;
        _mainProc = _mainModule.Body.GetILProcessor();
	    
        var mainParams = new ParameterDefinition("args", ParameterAttributes.None, Asm.MainModule.TypeSystem.String.MakeArrayType());
        _mainModule.Parameters.Add(mainParams);
        
        Gen();
    }

    private void InitDs()
    {
	    _funs = new Dictionary<string, MethodDefinition>();
	    _funsProcs = new Dictionary<string, ILProcessor>();
	    _paramsDefinitions = new Dictionary<string, Tuple<int, ParameterDefinition, Type>>();
    }
    
    public void Gen()
    {
	    // Actions? actions = action._actions;
	    // MainRoutine? mainRoutine = null;

	    // while (actions != null)
	    // {
		   //  GenerateAction(action);
		   //  action = actions._action;
		   //  actions = action._actions;
	    // }
	    // GenerateAction(action);

	    _mainProc.Emit(OpCodes.Call, _mainRoutineModule);
	    _mainProc.Emit(OpCodes.Ret);

	    var ctorMethod = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName, Asm.MainModule.TypeSystem.Void);
	    _typeDef.Methods.Add(ctorMethod);
	    var ctorProc = ctorMethod.Body.GetILProcessor();
	    ctorProc.Emit(OpCodes.Ldarg_0);
	    ctorProc.Emit(OpCodes.Call, Asm.MainModule.ImportReference(TypeHelpers.DefaultCtorFor(_typeDef.BaseType)));
	    ctorProc.Emit(OpCodes.Ret);
	    Asm.EntryPoint = _mainModule;
      
	    Asm.Write(_path);
	    File.Copy(
		    Path.ChangeExtension(typeof(Parser).Assembly.Location, ".runtimeconfig.json"),
		    Path.ChangeExtension(_path, ".runtimeconfig.json"),
		    true);
    }
    
    public void GenerateMainModule(MethodDefinition md, JsonElement module)
    {
        string name = "main";
        
        _typeDef.Methods.Add(md);
        md.Body.InitLocals = true;
        var proc = md.Body.GetILProcessor();
        
        _funs.Add(name, md);
        _funsProcs.Add(name, proc);
        
        var statements = module.GetProperty("Entry").GetProperty("Seq").GetProperty("Statements"); // arr
        GenerateStatements(statements, md, proc);

        proc.Emit(OpCodes.Ret);
    }

    public void GenerateStatements(JsonElement statements, MethodDefinition md, ILProcessor proc)
    {
	    for (int i = 0; i < statements.GetArrayLength(); i++)
	    {
		    GenerateStatement(statements[i], null, proc, md);
	    }
    }
    
    /// GenerateBody
    public void GenerateStatement(JsonElement stmt, TypeReference? returnType, ILProcessor proc, MethodDefinition md)
    {
        if (stmt.TryGetProperty("D", out JsonElement decl))
        {
            ParseDecl(decl, md, proc);
            return;
        }
        
        if (stmt.TryGetProperty("LInc", out JsonElement lInc))
        {
	        ParseLIncDec(lInc, proc, true);
	        return;
        }
        
        if (stmt.TryGetProperty("LDec", out JsonElement lDec))
        {
	        ParseLIncDec(lDec, proc, false);
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
        
        // ADD ANYTHING WITH "X" HERE
        
        if (stmt.TryGetProperty("X", out JsonElement ex)) // BE CAREFUL HERE BECAUSE EXCEPTION HAS "X" ONLY
        {
	        ParseException(ex, proc);
	        return;
        }
        
        Print("no");
    }

    public void ParseLIncDec(JsonElement x, ILProcessor proc, bool isInc)
    {
	    var exprBase = x.GetProperty("ExprBase");
	    var type = exprBase.GetProperty("Typ").GetProperty("Name").GetString();
	    Debug.Assert(type != null, nameof(type) + " != null");
	    
	    var name = exprBase.GetProperty("Name").GetString();
	    proc.Emit(OpCodes.Ldloc, Vars[name!]);
	    proc.Emit(OpCodes.Dup);
	    proc.Emit(OpCodes.Ldc_I4_1);
	    
	    if (isInc)
	    {
		    proc.Emit(OpCodes.Add);
	    }
	    else
	    {
		    proc.Emit(OpCodes.Sub);
	    }
	    
	    proc.Emit(OpCodes.Stloc, Vars[name!]);
	    proc.Emit(OpCodes.Pop);
    }

    public void ParseException(JsonElement x, ILProcessor proc)
    {
	    Value.GenerateValue(x, proc);
	    proc.Emit(OpCodes.Newobj, Asm.MainModule.ImportReference(TypeHelpers.ResolveMethod(
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
	    
	    var switchCondition = new VariableDefinition(TypesReferences[type!]);
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
	    var condDef = new VariableDefinition(Asm.MainModule.TypeSystem.Boolean);
	    md.Body.Variables.Add(condDef);
	    GenerateCondition(cond, proc, condDef);
	    
	    // int i = 0;
	    var var_i = new VariableDefinition(Asm.MainModule.TypeSystem.Int32);
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
		    GenerateStatement(statements[i], null, proc, md);
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

    // public void ParseValue(JsonElement value, string name, string type, MethodDefinition md, ILProcessor proc)
    // {
	   //  var vd = new VariableDefinition(GetTypeRef(type));
	   //  _vars.Add(name, vd);
	   //  
	   //  md.Body.Variables.Add(vd);
	   //  
	   //  new Expr(value).GenerateExpr(proc);
	   //  proc.Emit(OpCodes.Stloc, vd);
	   //  GeneratePrint(vd, type, proc);
	   //  
	   //  // if (type!.Equals("Цел64"))
	   //  // {
		  //  //  long value = value.GetProperty("IntVal").GetInt64();
		  //  //  // _generator.GenerateInt64(value);
	   //  // }
    // }

    // public void GenerateOperation(JsonElement operation, string type, ILProcessor proc)
    // {
	   //  // x // operation
	   //  // y // operand
	   //  // op // operator
	   //  if (operation.TryGetProperty("X", out JsonElement x) && operation.TryGetProperty("Y", out JsonElement y) && 
	   //      operation.TryGetProperty("Op", out JsonElement op))
	   //  {
		  //   GenerateOperand(y, type, proc);
		  //   
		  //   if (operation.TryGetProperty("X", out JsonElement xx) && operation.TryGetProperty("Y", out JsonElement xy) && 
		  //       operation.TryGetProperty("Op", out JsonElement xop))
		  //   {
			 //    GenerateOperand(xy, type, proc);
			 //    GenerateOperator(op.GetInt32(), proc);
			 //    
			 //    GenerateOperation(xx, type, proc);
			 //    GenerateOperator(xop.GetInt32(), proc);
			 //    return;
		  //   }
		  //   
		  //   GenerateOperation(x, type, proc);
		  //   GenerateOperator(op.GetInt32(), proc);
		  //   return;
	   //  }
	   //  
	   //  GenerateOperand(operation, type, proc);
    // }
    
    // public void GenerateOperand(JsonElement operand, string type, ILProcessor proc)
    // {
	   //  // x // operation
	   //  // y // operand
	   //  // op // operator
	   //  if (operand.TryGetProperty("X", out JsonElement x) && operand.TryGetProperty("Y", out JsonElement y) &&
	   //      operand.TryGetProperty("Op", out JsonElement op))
	   //  { // if operation
		  //   GenerateOperation(operand, type, proc); 
		  //   return;
	   //  }
	   //  
	   //  // if single
	   //  var opType = operand.GetProperty("Typ").GetProperty("Name");
    //
	   //  if (opType.Equals("Цел64")) // to do change all int32 to int64 (int32, Ldc_I4, etc)
	   //  {
		  //   proc.Emit(OpCodes.Ldc_I4, operand.GetProperty("IntVal").GetInt32());
	   //  }
	   //  // else if (type.Equals("Лог"))
	   //  // {
		  //  //  
	   //  // }
	   //  
	   //  // if (type.Equals(_asm.MainModule.TypeSystem.Int32))
	   //  // {
		  //  //  proc.Emit(OpCodes.Ldc_I4, Int32.Parse(value));
	   //  // }
	   //  // else if (type.Equals(_asm.MainModule.TypeSystem.Double))
	   //  // {
		  //  //  proc.Emit(OpCodes.Ldc_R8, Double.Parse(value, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo));
	   //  // }
	   //  // else if (type.Equals(_asm.MainModule.TypeSystem.Boolean))
	   //  // {
		  //  //  if (value.Equals("true"))
		  //  //  {
			 //  //   proc.Emit(OpCodes.Ldc_I4, 1);
		  //  //  }
		  //  //  else if (value.Equals("false"))
		  //  //  {
			 //  //   proc.Emit(OpCodes.Ldc_I4, 0);
		  //  //  }
	   //  // }
	   //  
	   //  // if single variable
	   //  
	   //  // if (_vars.Keys.Contains(name))
	   //  // {
		  //  //  proc.Emit(OpCodes.Ldloc, _vars[name]);
	   //  // }
	   //  // else
	   //  // {
		  //  //  GenerateLDARG(_paramsDefinitions[name].Item1, proc);
	   //  // }
	   //  //
	   //  // if (type.Equals("Цел64"))
	   //  // {
		  //  //  proc.Emit(OpCodes.Ldelem_I4);
	   //  // }
	   //  
	   //  // Expression index = operand._single._variable._arrayType._arrayType._expression;
	   //  // GenerateExpression(index, proc);
	   //  // Type type = _varsTypes[name]._arrayType._type;
	   //  // if (type._primitiveType._isInt)
	   //  // {
		  //  //  proc.Emit(OpCodes.Ldelem_I4);
	   //  // }
	   //  // else if (type._primitiveType._isBoolean)
	   //  // {
		  //  //  proc.Emit(OpCodes.Ldelem_U1);
	   //  // }
	   //  // else if (type._primitiveType._isReal)
	   //  // {
		  //  //  proc.Emit(OpCodes.Ldelem_R8);
	   //  // }
	   //  
	   //  //// else EmitValue(operand._single._value, proc, GetTypeRef(operand._single._type));
    // }
    
    public void GenerateLDARG(int i, ILProcessor proc)
    {
	    if (i == 0)
	    {
		    proc.Emit(OpCodes.Ldarg_0);
	    }
	    else if (i == 1)
	    {
		    proc.Emit(OpCodes.Ldarg_1);
	    }
	    else if (i == 2)
	    {
		    proc.Emit(OpCodes.Ldarg_2);
	    }
	    else if (i == 3)
	    {
		    proc.Emit(OpCodes.Ldarg_3);
	    }
	    else
	    {
		    proc.Emit(OpCodes.Ldarg, i);
	    }
    }

    public void GenerateVarDecl(JsonElement decl, MethodDefinition md, ILProcessor proc)
    {  
	    JsonElement descBase = decl.GetProperty("DeclBase");
	    string? name = descBase.GetProperty("Name").GetString();
	    string? type = descBase.GetProperty("Typ").GetProperty("Name").GetString();
	    
	    JsonElement value = decl.GetProperty("Init");
	    
	    var vd = new VariableDefinition(TypesReferences[type!]);
	    Vars.Add(name!, vd);
	    
	    md.Body.Variables.Add(vd);

	    if (type!.Equals("Пусто"))
	    {
		    proc.Emit(OpCodes.Initobj, TypesReferences[type]);
	    }
	    else
	    {
		    new Expr(value).GenerateExpr(proc);
		    proc.Emit(OpCodes.Stloc, vd);
	    }
	    
	    Out.GeneratePrint(vd, type!, proc);
	    
	    
	    // else if (type._arrayType != null)
	    // {
		   //  ArrayType at = type._arrayType;
		   //  Expression exp = at._expression; // length
		   //  Type t = at._type;
	    //
		   //  var arr = new VariableDefinition(GetTypeRef(t).MakeArrayType());
		   //  md.Body.Variables.Add(arr);
		   //  GenerateOperation(exp, proc);
		   //  proc.Emit(OpCodes.Newarr, GetTypeRef(t));
		   //  proc.Emit(OpCodes.Stloc, arr);
		   //  
		   //  _vars.Add(name, arr);
		   //  // _varsTypes.Add(name, type);
	    // }
	    // else if (type._userType != null && _typeDeclarations.Keys.Contains(type._userType._name))
	    // {
		   //  VariableDeclaration vewVarDecl = new VariableDeclaration(varDecl._identifier,
			  //   _typeDeclarations[type._userType._name], varDecl._value);
		   //  GenerateVarDecl(vewVarDecl, md, proc, name);
	    // }
	    // else if (type._userType != null)
	    // {
		   //  RecordType recordType = null;
		   //  Type t = null;
		   //  foreach (var record in _records)
		   //  {
			  //   if (record._identifier._name.Equals(type._userType._name))
			  //   {
				 //    recordType = record._type._recordType;
				 //    t = record._type;
				 //    break;
			  //   }
		   //  }
	    //
		   //  var recordDefinition = new VariableDefinition(_recTypeDefinitions[type._userType._name]);
		   //  md.Body.Variables.Add(recordDefinition);
		   //  proc.Emit(OpCodes.Newobj, _constructors[type._userType._name]);
		   //  proc.Emit(OpCodes.Stloc, recordDefinition);
	    //
		   //  // var recordDefinition = new VariableDefinition(_recTypeDefinitions[type._userType._name]);
		   //  // md.Body.Variables.Add(recordDefinition);
		   //  if (value != null)
		   //  {
			  //   int i = 0;
			  //   VariableDeclaration vd = recordType._variableDeclaration;
			  //   VariableDeclarations vds = recordType._variableDeclarations;
			  //   Expressions expressions = value._expressions;
			  //   Expression expression = null;
			  //   List<FieldDefinition> fieldDefs = _recFieldDefinitions[type._userType._name];
			  //   while (expressions != null)
			  //   {
				 //    expression = expressions._expression;
				 //    expressions = expressions._expressions;
				 //    ProcessField(expression, recordDefinition, proc, fieldDefs[i]);
	    //
				 //    if (vd._type._primitiveType._isInt || vd._type._primitiveType._isBoolean)
				 //    {
					//     proc.Emit(OpCodes.Ldloc, recordDefinition);
					//     proc.Emit(OpCodes.Ldfld, fieldDefs[i]);
					//     proc.Emit(OpCodes.Call,
					// 	    _asm.MainModule.ImportReference(TypeHelpers.ResolveMethod(typeof(System.Console),
					// 		    "WriteLine",
					// 		    System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.Static |
					// 		    System.Reflection.BindingFlags.Public, "System.Int32")));
				 //    }
				 //    else if (vd._type._primitiveType._isReal)
				 //    {
					//     proc.Emit(OpCodes.Ldloc, recordDefinition);
					//     proc.Emit(OpCodes.Ldfld, fieldDefs[i]);
					//     proc.Emit(OpCodes.Call,
					// 	    _asm.MainModule.ImportReference(TypeHelpers.ResolveMethod(typeof(System.Console),
					// 		    "WriteLine",
					// 		    System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.Static |
					// 		    System.Reflection.BindingFlags.Public, "System.Double")));
				 //    }
	    //
				 //    if (vds != null)
				 //    {
					//     vd = vds._variableDeclaration;
					//     vds = vds._variableDeclarations;
				 //    }
	    //
				 //    i++;
			  //   }
		   //  }
	    //
		   //  // _varsTypes.Add(name, t);
		   //  _vars.Add(name, recordDefinition);
	    // }
    }
    
    
    
    /// <summary>
    /// ------------------------------------------------------------------------------------------------
    /// </summary>
    

    private void ParseImport(JsonElement import)
    {
        
    }

    private void ParseDecl(JsonElement decl, MethodDefinition md, ILProcessor proc)
    {
        // todo if var decl
        GenerateVarDecl(decl, md, proc); 
    }

    private void ParseAssignment(JsonElement l, JsonElement r, ILProcessor proc)
    {
	    string? name = l.GetProperty("Name").GetString();
	    string? type = l.GetProperty("Typ").GetProperty("Name").GetString();

	    new Expr(r).GenerateExpr(proc);
	    proc.Emit(OpCodes.Stloc, Vars[name!]);

	    Out.GeneratePrint(Vars[name!], type!, proc);
    }

    private void Print(Object o)
    {
        Console.WriteLine(o);
    }
}