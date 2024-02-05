namespace ConsoleApp1.parser;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

using Cecilifier.Runtime;

public class Generator
{
    private AssemblyDefinition _asm;
    private TypeDefinition _typeDef;
    private MethodDefinition _mainModule;
    private ILProcessor _mainProc;
    
    private const string _path = "../../../exe/code.exe";

    public Generator()
    {
        var mp = new ModuleParameters { Architecture = TargetArchitecture.AMD64, Kind =  ModuleKind.Console, ReflectionImporterProvider = new SystemPrivateCoreLibFixerReflectionProvider() };
        _asm = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("Program", Version.Parse("1.0.0.0")), Path.GetFileName(_path), mp);
	    
        _typeDef = new TypeDefinition("", "Program", TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Public, _asm.MainModule.TypeSystem.Object);
        _asm.MainModule.Types.Add(_typeDef);
    }

    public void GenerateInt64(long value)
    {
        Print(value);
    }
    
    private void Print(Object o)
    {
        Console.WriteLine(o);
    }
    
}