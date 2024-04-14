using System.Diagnostics;
using System.Text.Json;
using Cecilifier.Runtime;
using ConsoleApp1.generator.expr;
using ConsoleApp1.generator.functions;
using ConsoleApp1.generator.print;
using ConsoleApp1.generator.statements;

namespace ConsoleApp1.parser;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

public class Parser	
{
    private readonly JsonElement _ast;
    
    public static AssemblyDefinition Asm;
    public TypeDefinition MainClassTypeDef;
    private MethodDefinition _mainModule;
    private ILProcessor _mainProc;
    
    private const string _path = "../../../exe/code.exe";
    
    private MethodDefinition _mainRoutineModule;
    
    // private Dictionary<string, MethodDefinition> _funs;
    // private Dictionary<string, ILProcessor> _funsProcs;
    // private Dictionary<string, Type> _varsTypes;
    private Dictionary<string, Tuple<int, ParameterDefinition, Type>> _paramsDefinitions;

    public static Dictionary<string, TypeReference> TypesReferences;
    
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
	    
        MainClassTypeDef = new TypeDefinition("", "Program", TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Public, Asm.MainModule.TypeSystem.Object);
        Asm.MainModule.Types.Add(MainClassTypeDef);

        GenerateGlobalFunctions(MainClassTypeDef, _ast);
        
        _mainRoutineModule = new MethodDefinition("mainRoutineModule", MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig, Asm.MainModule.TypeSystem.Void);
        GenerateMainModule(_mainRoutineModule, _ast);
        
        _mainModule = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, Asm.MainModule.TypeSystem.Void);
        MainClassTypeDef.Methods.Add(_mainModule);
        _mainModule.Body.InitLocals = true;
        _mainProc = _mainModule.Body.GetILProcessor();
	    
        var mainParams = new ParameterDefinition("args", ParameterAttributes.None, Asm.MainModule.TypeSystem.String.MakeArrayType());
        _mainModule.Parameters.Add(mainParams);
        
        Gen();
    }

    private void InitDs()
    {
	    // _funs = new Dictionary<string, MethodDefinition>();
	    // _funsProcs = new Dictionary<string, ILProcessor>();
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
	    MainClassTypeDef.Methods.Add(ctorMethod);
	    var ctorProc = ctorMethod.Body.GetILProcessor();
	    ctorProc.Emit(OpCodes.Ldarg_0);
	    ctorProc.Emit(OpCodes.Call, Asm.MainModule.ImportReference(TypeHelpers.DefaultCtorFor(MainClassTypeDef.BaseType)));
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
        
        MainClassTypeDef.Methods.Add(md);
        md.Body.InitLocals = true;
        var proc = md.Body.GetILProcessor();
        
        // _funs.Add(name, md);
        // _funsProcs.Add(name, proc);
        
        var statements = module.GetProperty("Entry").GetProperty("Seq").GetProperty("Statements"); // arr
        Statement.GenerateStatements(statements, md, proc);

        proc.Emit(OpCodes.Ret);
    }

    public void GenerateGlobalFunctions(TypeDefinition mainClass, JsonElement module)
    {
	    JsonElement functions = module.GetProperty("Decl"); // arr todo: here are not only functions (also global vars and etc)
	    for (int i = 0; i < functions.GetArrayLength(); i++)
	    {
		    Function function = new Function(functions[i], this);
		    function.GenerateFunction();
		    // GenerateFunction(functions[i], mainClass);
	    }
    }
    
    // todo here is void only
    // public void GenerateFunction(JsonElement fun, TypeDefinition mainClass)
    // {
	   //  string? name = fun.GetProperty("DeclBase").GetProperty("Name").GetString();
	   //  
	   //  // generate fun decl
	   //  var funMd = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, Asm.MainModule.TypeSystem.Void);
	   //  mainClass.Methods.Add(funMd);
	   //  funMd.Body.InitLocals = true;
	   //  var funProc = funMd.Body.GetILProcessor();
	   //  
	   //  _funs.Add(name!, funMd);
	   //  
	   //  JsonElement p = fun.GetProperty("DeclBase").GetProperty("Typ").GetProperty("Params"); // [] or null
	   //  Parameters parameters = new Parameters(p);
	   //  parameters.GenerateParameters(); // todo
	   //  
	   //  JsonElement returnType = fun.GetProperty("DeclBase").GetProperty("Typ").GetProperty("ReturnTyp"); // Obj or null
	   //  // todo generate returnType
	   //  
	   //  JsonElement statements = fun.GetProperty("Seq").GetProperty("Statements");
	   //  // generate stmts
	   //  GenerateStatements(statements, funMd, funProc);
	   //  
	   //  funProc.Emit(OpCodes.Ret);
    // }

    
    
    /// GenerateBody
    

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

    
    
    
    
    /// <summary>
    /// ------------------------------------------------------------------------------------------------
    /// </summary>
    

    private void ParseImport(JsonElement import)
    {
        
    }

    

    
}