using System.Text.Json;
using ConsoleApp1.generator.expr;
using ConsoleApp1.generator.functions;
using ConsoleApp1.generator.print;
using ConsoleApp1.parser;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ConsoleApp1.generator.statements;

public class Declaration(JsonElement decl, MethodDefinition md, ILProcessor proc)
{
    public static bool IsDecl(JsonElement stmt)
    {
        return stmt.TryGetProperty("D", out _);
    }
    
    public static Declaration GetDecl(JsonElement stmt, MethodDefinition md, ILProcessor proc)
    {
        return new Declaration(stmt.GetProperty("D"), md, proc);
    }

    public void Parse()
    {
        GenerateVarDecl(); 
    }
    
    public void GenerateVarDecl()
    {  
	    JsonElement descBase = decl.GetProperty("DeclBase");
	    string? name = descBase.GetProperty("Name").GetString();
	    JsonElement typeName;
	    
	    bool isFun = descBase.GetProperty("Typ").TryGetProperty("TypeName", out typeName);
	    if (!isFun)
	    {
		    typeName = descBase.GetProperty("Typ").GetProperty("Name");
	    }
	    
	    string? type = typeName.GetString();
	    JsonElement value = decl.GetProperty("Init");
	    
	    var vd = new VariableDefinition(Parser.TypesReferences[type!]);
	    Statement.Vars.Add(name!, vd);
	    
	    md.Body.Variables.Add(vd);

	    if (type!.Equals("Пусто"))
	    {
		    proc.Emit(OpCodes.Initobj, Parser.TypesReferences[type]);
	    }
	    else if (isFun)
	    {
		    string? funName = value.GetProperty("Call").GetProperty("Name").GetString();
		    proc.Emit(OpCodes.Call, Function.Funs[funName!]);
		    proc.Emit(OpCodes.Stloc, vd);
	    }
	    else
	    {
		    new Expr(value).GenerateExpr(proc);
		    proc.Emit(OpCodes.Stloc, vd);
	    }
	    
	    Out.GeneratePrint(vd, type, proc);
	    
	    
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
}