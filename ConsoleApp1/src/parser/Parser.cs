using System.Text.Json;
using Cecilifier.Runtime;

namespace ConsoleApp1.parser;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

public class Parser
{
    private readonly JsonElement _ast;
    // private Generator _generator;
    
    private AssemblyDefinition _asm;
    private TypeDefinition _typeDef;
    private MethodDefinition _mainModule;
    private ILProcessor _mainProc;
    
    private const string _path = "../../../exe/code.exe";
    
    private MethodDefinition _mainRoutineModule;
    
    private Dictionary<string, MethodDefinition> _funs;
    private Dictionary<string, ILProcessor> _funsProcs;
    // private Dictionary<string, Type> _varsTypes;
    private Dictionary<string, VariableDefinition> _vars;
    private Dictionary<string, Tuple<int, ParameterDefinition, Type>> _paramsDefinitions;
    
    public Parser(string path)
    {
        string data = File.ReadAllText(path);
        JsonDocument doc = JsonDocument.Parse(data);
        JsonElement root = doc.RootElement;
        _ast = root;

        InitDs();

        // _generator = new Generator();
        var mp = new ModuleParameters { Architecture = TargetArchitecture.AMD64, Kind =  ModuleKind.Console, ReflectionImporterProvider = new SystemPrivateCoreLibFixerReflectionProvider() };
        _asm = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("Program", Version.Parse("1.0.0.0")), Path.GetFileName(_path), mp);
	    
        _typeDef = new TypeDefinition("", "Program", TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Public, _asm.MainModule.TypeSystem.Object);
        _asm.MainModule.Types.Add(_typeDef);
        
        _mainRoutineModule = new MethodDefinition("mainRoutineModule", MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig, _asm.MainModule.TypeSystem.Void);
        GenerateMainModule(_mainRoutineModule, _ast);
        
        _mainModule = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, _asm.MainModule.TypeSystem.Void);
        _typeDef.Methods.Add(_mainModule);
        _mainModule.Body.InitLocals = true;
        _mainProc = _mainModule.Body.GetILProcessor();
	    
        var mainParams = new ParameterDefinition("args", ParameterAttributes.None, _asm.MainModule.TypeSystem.String.MakeArrayType());
        _mainModule.Parameters.Add(mainParams);
        
        Gen();
    }

    private void InitDs()
    {
	    _funs = new Dictionary<string, MethodDefinition>();
	    _funsProcs = new Dictionary<string, ILProcessor>();
	    _vars = new Dictionary<string, VariableDefinition>();
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

	    var ctorMethod = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName, _asm.MainModule.TypeSystem.Void);
	    _typeDef.Methods.Add(ctorMethod);
	    var ctorProc = ctorMethod.Body.GetILProcessor();
	    ctorProc.Emit(OpCodes.Ldarg_0);
	    ctorProc.Emit(OpCodes.Call, _asm.MainModule.ImportReference(TypeHelpers.DefaultCtorFor(_typeDef.BaseType)));
	    ctorProc.Emit(OpCodes.Ret);
	    _asm.EntryPoint = _mainModule;
      
	    _asm.Write(_path);
	    File.Copy(
		    Path.ChangeExtension(typeof(Parser).Assembly.Location, ".runtimeconfig.json"),
		    Path.ChangeExtension(_path, ".runtimeconfig.json"),
		    true);
    }
    
    public void GenerateMainModule(MethodDefinition funModule, JsonElement module)
    {
        string name = "main";
        
        _typeDef.Methods.Add(funModule);
        funModule.Body.InitLocals = true;
        var funProc = funModule.Body.GetILProcessor();
        
        _funs.Add(name, funModule);
        _funsProcs.Add(name, funProc);
        
        var statements = module.GetProperty("Entry").GetProperty("Seq").GetProperty("Statements"); // arr
        // GenerateBody(statements, null, funProc, funModule);
        
        for (int i = 0; i < statements.GetArrayLength(); i++)
        {
            // ParseStatement(statements[i]);
            GenerateStatement(statements[i], null, funProc, funModule);
        }

        funProc.Emit(OpCodes.Ret);
        
    }
    
    /// GenerateBody
    public void GenerateStatement(JsonElement stmt, TypeReference returnType, ILProcessor proc, MethodDefinition md)
    {
        if (stmt.TryGetProperty("D", out JsonElement decl))
        {
            // declaration processing
            ParseDecl(decl, md, proc);
            return;
        }
        
        if (stmt.TryGetProperty("L", out JsonElement lAssig) && stmt.TryGetProperty("R", out JsonElement rAssig))
        {
            // assignment processing
            ParseAssignment(lAssig, rAssig, proc);
            return;
        }
        
        Print("no");
    }

    public void ParseValue(JsonElement value, string name, string type, MethodDefinition md, ILProcessor proc)
    {
	    var vd = new VariableDefinition(GetTypeRef(type));
	    _vars.Add(name, vd);
	    
	    md.Body.Variables.Add(vd);
	    
	    GenerateOperation(value, name, type, proc);
	    proc.Emit(OpCodes.Stloc, vd);
	    GeneratePrint(vd, type, proc);
	    
	    // if (type!.Equals("Цел64"))
	    // {
		   //  long value = value.GetProperty("IntVal").GetInt64();
		   //  // _generator.GenerateInt64(value);
	    // }
    }

    public void GenerateOperation(JsonElement operation, string name, string type, ILProcessor proc)
    {
	    // x // operation
	    // y // operand
	    // op // operator
	    if (operation.TryGetProperty("X", out JsonElement x) && operation.TryGetProperty("Y", out JsonElement y) && 
	        operation.TryGetProperty("Op", out JsonElement op))
	    {
		    GenerateOperand(y, name, type, proc);
		    
		    if (operation.TryGetProperty("X", out JsonElement xx) && operation.TryGetProperty("Y", out JsonElement xy) && 
		        operation.TryGetProperty("Op", out JsonElement xop))
		    {
			    GenerateOperand(xy, name, type, proc);
			    GenerateOperator(op.GetInt32(), proc);
			    
			    GenerateOperation(xx, name, type, proc);
			    GenerateOperator(xop.GetInt32(), proc);
			    return;
		    }
		    
		    GenerateOperation(x, name, type, proc);
		    GenerateOperator(op.GetInt32(), proc);
		    return;
	    }
	    
	    GenerateOperand(operation, name, type, proc);
    }
    
    public void GenerateOperand(JsonElement operand, string name, string type, ILProcessor proc)
    {
	    // x // operation
	    // y // operand
	    // op // operator
	    if (operand.TryGetProperty("X", out JsonElement x) && operand.TryGetProperty("Y", out JsonElement y) &&
	        operand.TryGetProperty("Op", out JsonElement op))
	    { // if operation
		    GenerateOperation(operand, name, type, proc); 
		    return;
	    }
	    
	    // if single

	    if (type.Equals("Цел64")) // todo change all int32 to int64 (int32, Ldc_I4, etc)
	    {
		    proc.Emit(OpCodes.Ldc_I4, operand.GetProperty("IntVal").GetInt32());
	    }
	    
	    // if (type.Equals(_asm.MainModule.TypeSystem.Int32))
	    // {
		   //  proc.Emit(OpCodes.Ldc_I4, Int32.Parse(value));
	    // }
	    // else if (type.Equals(_asm.MainModule.TypeSystem.Double))
	    // {
		   //  proc.Emit(OpCodes.Ldc_R8, Double.Parse(value, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo));
	    // }
	    // else if (type.Equals(_asm.MainModule.TypeSystem.Boolean))
	    // {
		   //  if (value.Equals("true"))
		   //  {
			  //   proc.Emit(OpCodes.Ldc_I4, 1);
		   //  }
		   //  else if (value.Equals("false"))
		   //  {
			  //   proc.Emit(OpCodes.Ldc_I4, 0);
		   //  }
	    // }
	    
	    // if single variable
	    
	    // if (_vars.Keys.Contains(name))
	    // {
		   //  proc.Emit(OpCodes.Ldloc, _vars[name]);
	    // }
	    // else
	    // {
		   //  GenerateLDARG(_paramsDefinitions[name].Item1, proc);
	    // }
	    //
	    // if (type.Equals("Цел64"))
	    // {
		   //  proc.Emit(OpCodes.Ldelem_I4);
	    // }
	    
	    // Expression index = operand._single._variable._arrayType._arrayType._expression;
	    // GenerateExpression(index, proc);
	    // Type type = _varsTypes[name]._arrayType._type;
	    // if (type._primitiveType._isInt)
	    // {
		   //  proc.Emit(OpCodes.Ldelem_I4);
	    // }
	    // else if (type._primitiveType._isBoolean)
	    // {
		   //  proc.Emit(OpCodes.Ldelem_U1);
	    // }
	    // else if (type._primitiveType._isReal)
	    // {
		   //  proc.Emit(OpCodes.Ldelem_R8);
	    // }
	    
	    //// else EmitValue(operand._single._value, proc, GetTypeRef(operand._single._type));
    }
    
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

    public void GenerateOperator(int op, ILProcessor proc)
    {
	    // if (op._mathematicalOperator != null)
		   //  GenerateMathOp(op._mathematicalOperator, proc);
	    // else if (op._comparisonOperator != null)
		   //  GenerateCompOp(op._comparisonOperator, proc);
	    // else if(op._logicalOperator != null)
		   //  GenerateLogicOp(op._logicalOperator, proc);
    }
    
    public TypeReference GetTypeRef(string type)
    {
	    if (type.Equals("Цел64"))
	    {
		    return _asm.MainModule.TypeSystem.Int32;
	    }
	    
	    // if (type._primitiveType._isReal)
	    // {
		   //  return _asm.MainModule.TypeSystem.Double;
	    // }
	    // if (type._primitiveType._isBoolean)
	    // {
		   //  return _asm.MainModule.TypeSystem.Boolean;
	    // }
	    
	    return null;
    }

    public void GenerateVarDecl(JsonElement decl, MethodDefinition md, ILProcessor proc)
    {  
	    string? name = decl.GetProperty("Name").GetString();
	    string? type = decl.GetProperty("Typ").GetProperty("Name").GetString();
	    Print(name);
	    Print(type);
	    
	    JsonElement value = decl.GetProperty("Init");
	    ParseValue(value, name, type, md, proc);
	    
	    
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

    public void GenerateInt64(long value)
    {
        Print(value);
    }
    
    
    
    
    /// <summary>
    /// ------------------------------------------------------------------------------------------------
    /// </summary>

    public void StartParsing()
    {
        ParseModule(_ast);
    }

    private void ParseModule(JsonElement module)
    {
        var imports = module.GetProperty("Imports"); // arr
        for (int i = 0; i < imports.GetArrayLength(); i++)
        {
            ParseImport(imports[i]);
        }
        
        var decls = module.GetProperty("Decls"); // arr
        for (int i = 0; i < decls.GetArrayLength(); i++)
        {
            // ParseDecl(decls[i]);
        }
        
        var statements = module.GetProperty("Entry").GetProperty("Seq").GetProperty("Statements"); // arr
        for (int i = 0; i < statements.GetArrayLength(); i++)
        {
            // GenerateStatement(statements[i]);
        }
    }

    private void ParseImport(JsonElement import)
    {
        
    }

    private void ParseDecl(JsonElement decl, MethodDefinition md, ILProcessor proc)
    {
        // todo if var decl
        GenerateVarDecl(decl, md, proc); 
        // string? name = decl.GetProperty("Name").GetString();
        // string? type = decl.GetProperty("Typ").GetProperty("Name").GetString();
        // Print(name);
        // Print(type);
        //
        // JsonElement init = decl.GetProperty("Init");
        // ParseExpr(init);
    }

    private void ParseExpr(JsonElement expr)
    {
        string? type = expr.GetProperty("Typ").GetProperty("Name").GetString();
        
        // x // operation
        // y // operand
        // op // operator
        if (expr.TryGetProperty("X", out JsonElement x) && expr.TryGetProperty("Y", out JsonElement y) && 
            expr.TryGetProperty("Op", out JsonElement op))
        {
            ParseExpr(x);
            int opCode = op.GetInt32();
            ParseExpr(y);
            return;
        }
        
        if (type!.Equals("Цел64"))
        {
            long value = expr.GetProperty("IntVal").GetInt64();
            // _generator.GenerateInt64(value);
        }
    }
    
    public void GeneratePrint(VariableDefinition varDef, string type, ILProcessor proc)
    {
	    var origType = "System.Int32"; // todo change
	    
	    proc.Emit(OpCodes.Ldloc, varDef);
	    proc.Emit(OpCodes.Call, _asm.MainModule.ImportReference(TypeHelpers.ResolveMethod(typeof(System.Console), "WriteLine",System.Reflection.BindingFlags.Default|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public, origType)));
    }

    private void ParseAssignment(JsonElement l, JsonElement r, ILProcessor proc)
    {
	    string? name = l.GetProperty("Name").GetString();
	    string? type = l.GetProperty("Typ").GetProperty("Name").GetString();

	    GenerateOperation(r, name!, type!, proc);
	    proc.Emit(OpCodes.Stloc, _vars[name!]);

	    GeneratePrint(_vars[name!], type!, proc);
    }

    private void Print(Object o)
    {
        Console.WriteLine(o);
    }
}