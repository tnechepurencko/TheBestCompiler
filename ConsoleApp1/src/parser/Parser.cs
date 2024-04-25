using System.Text.Json;
using Cecilifier.Runtime;
using ConsoleApp1.generator.expr;
using ConsoleApp1.generator.functions;
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
    
    private const string Path = "../../../exe/code.exe";
    private bool _generateCCtor = false;
    
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
        Asm = AssemblyDefinition.CreateAssembly(and, System.IO.Path.GetFileName(Path), mp);

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

        GenerateGlobals(_ast);
        
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
	    _mainProc.Emit(OpCodes.Call, _mainRoutineModule);
	    _mainProc.Emit(OpCodes.Ret);

	    GenerateCtor();
	    GenerateCCtor();
	    Asm.EntryPoint = _mainModule;
      
	    Asm.Write(Path);
	    File.Copy(
		    System.IO.Path.ChangeExtension(typeof(Parser).Assembly.Location, ".runtimeconfig.json"),
		    System.IO.Path.ChangeExtension(Path, ".runtimeconfig.json"),
		    true);
    }

    public void GenerateCtor()
    {
	    var ctorMd = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName, Asm.MainModule.TypeSystem.Void);
	    MainClassTypeDef.Methods.Add(ctorMd);
	    
	    var ctorProc = ctorMd.Body.GetILProcessor();
	    ctorProc.Emit(OpCodes.Ldarg_0);
	    ctorProc.Emit(OpCodes.Call, Asm.MainModule.ImportReference(TypeHelpers.DefaultCtorFor(MainClassTypeDef.BaseType)));
	    ctorProc.Emit(OpCodes.Ret);
    }

    public void GenerateCCtor() // only if I have main class variables
    {
	    if (_generateCCtor)
	    {
		    
	    }
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

    public void GenerateGlobals(JsonElement module)
    {
	    JsonElement decls = module.GetProperty("Decl");
	    for (int i = 0; i < decls.GetArrayLength(); i++)
	    {
		    JsonElement type = decls[i].GetProperty("DeclBase").GetProperty("Typ");
		    if (type.TryGetProperty("Fields", out _)) // it is a class
		    {
			    // todo generate class
		    } else if (type.TryGetProperty("Params", out _)) // it is a fun
		    {
			    Function function = new Function(decls[i], this);
			    function.GenerateFunction();
		    } else if (type.TryGetProperty("ElementTyp", out _)) // it is an arr
		    {
			    _generateCCtor = true;
			    Vector vector = new Vector(decls[i]);
			    vector.GenerateVector();
		    }
	    }
    }

    // public void GenerateGlobalFunctions(TypeDefinition mainClass, JsonElement module)
    // {
	   //  JsonElement functions = module.GetProperty("Decl"); // arr todo: here are not only functions (also global vars and etc)
	   //  for (int i = 0; i < functions.GetArrayLength(); i++)
	   //  {
		  //   Function function = new Function(functions[i], this);
		  //   function.GenerateFunction();
		  //   // GenerateFunction(functions[i], mainClass);
	   //  }
    // }
}