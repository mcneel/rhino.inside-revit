using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Autodesk.Revit.Attributes;

namespace RhinoInside.Revit.AddIn.Commands
{
  public class LinkedScriptAssemblyBuilder
  {
    public LinkedScriptAssemblyBuilder()
    {
      Name = Guid.NewGuid().ToString();
      FileName = $"{Name}.dll";
      FileLocation = Path.Combine(Core.SwapFolder, "LinkedScripts");
      Directory.CreateDirectory(FileLocation);

#if NET
      AssmBuilder = AssemblyBuilder.DefineDynamicAssembly
      (
        new AssemblyName { Name = Name, Version = new Version(0, 1) },
        AssemblyBuilderAccess.Run
      );

      ModuleBuilder = AssmBuilder.DefineDynamicModule(Name);
#else
      AssmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly
      (
        new AssemblyName { Name = Name, Version = new Version(0, 1) },
        AssemblyBuilderAccess.RunAndSave,
        FileLocation
      );

      ModuleBuilder = AssmBuilder.DefineDynamicModule(Name, FileName);
#endif
    }

    public string Name { get; }
    public string FileLocation { get; }
    public string FileName { get; }
    public string FilePath => Path.Combine(FileLocation, FileName);

    readonly AssemblyBuilder AssmBuilder;
    readonly ModuleBuilder ModuleBuilder;

    public Assembly SaveAndLoad()
    {
#if NET
      var generator = new Lokad.ILPack.AssemblyGenerator();
      generator.GenerateAssembly(ModuleBuilder.Assembly, FilePath);
#else
      AssmBuilder?.Save(FileName);
#endif
      return Assembly.LoadFrom(FilePath);
    }

    public Type MakeScriptCommandType(LinkedScript script)
    {
      var typeBuilder = ModuleBuilder.DefineType
      (
        $"LinkedScriptCmd-{Guid.NewGuid()}",
        TypeAttributes.Public | TypeAttributes.Class,
        typeof(GrasshopperScriptCommand)
      );

      // Transaction(TransactionMode.Manual)
      typeBuilder.SetCustomAttribute
      (
        new CustomAttributeBuilder
        (
          typeof(TransactionAttribute).GetConstructor(new Type[] { typeof(TransactionMode) }),
          new object[] { TransactionMode.Manual }
        )
      );

      // Regeneration(RegenerationOption.Manual)
      typeBuilder.SetCustomAttribute
      (
        new CustomAttributeBuilder
        (
          typeof(RegenerationAttribute).GetConstructor(new Type[] { typeof(RegenerationOption) }),
          new object[] { RegenerationOption.Manual }
        )
      );

      // get GrasshopperScriptCommand(string scriptPath) const
      var baseConst = typeof(GrasshopperScriptCommand).GetConstructor
      (
        BindingFlags.Instance | BindingFlags.NonPublic, default,
        new Type[]
        {
          typeof(string) // "scriptPath"
        },
        null
      );

      // define a base contructor
      var defaultConst = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
      var gen = defaultConst.GetILGenerator();
      gen.Emit(OpCodes.Ldarg_0);                // load "this" onto stack
      gen.Emit(OpCodes.Ldstr, script.ScriptPath);// load "scriptPath"

      gen.Emit(OpCodes.Call, baseConst); // call script command constructor with values loaded to stack
      gen.Emit(OpCodes.Nop);                    // add a few NOPs
      gen.Emit(OpCodes.Ret);                    // return

      return typeBuilder.CreateType();
    }
  }
}
