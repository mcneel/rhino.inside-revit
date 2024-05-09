using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Autodesk.Revit.Attributes;

namespace RhinoInside.Revit.AddIn.Commands
{
  public class LinkedScriptAssembly
  {
    public LinkedScriptAssembly()
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

    public string Name { get; private set; }
    public string FileLocation { get; private set; }
    public string FileName { get; private set; }
    public string FilePath => Path.Combine(FileLocation, FileName);

    public AssemblyBuilder AssmBuilder { get; private set; }
    public ModuleBuilder ModuleBuilder { get; private set; }

    public void SaveAndLoad()
    {
#if NET
      var generator = new Lokad.ILPack.AssemblyGenerator();
      generator.GenerateAssembly(ModuleBuilder.Assembly, FilePath);
#else
      AssmBuilder?.Save(FileName);
#endif
      Assembly.LoadFrom(FilePath);
    }

    public Type MakeScriptCommandType(LinkedScript script)
    {
      var typeBuilder = ModuleBuilder.DefineType(
        $"LinkedScriptCmd-{Guid.NewGuid()}",
        TypeAttributes.Public | TypeAttributes.Class,
        typeof(LinkedScriptCommand)
        );

      // Transaction(TransactionMode.Manual)
      typeBuilder.SetCustomAttribute(
        new CustomAttributeBuilder(
                  typeof(TransactionAttribute).GetConstructor(new Type[] { typeof(TransactionMode) }),
                  new object[] { TransactionMode.Manual }
      ));

      //  Regeneration(RegenerationOption.Manual)
      typeBuilder.SetCustomAttribute(
        new CustomAttributeBuilder(
          typeof(RegenerationAttribute).GetConstructor(new Type[] { typeof(RegenerationOption) }),
          new object[] { RegenerationOption.Manual }
      ));

      // get GrasshopperScriptCommand(string scriptPath) const
      var ghScriptCmdConst = typeof(LinkedScriptCommand).GetConstructor(new Type[] {
        typeof(int),        // "scriptType"
        typeof(string)      // "scriptPath"
      });
      // define a base contructor
      var baseConst = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { });
      var gen = baseConst.GetILGenerator();
      gen.Emit(OpCodes.Ldarg_0);                // load "this" onto stack
                                                // load "scriptType"
      gen.Emit(OpCodes.Ldc_I4, (int) script.ScriptType);
      // load "scriptPath"
      gen.Emit(OpCodes.Ldstr, script.ScriptPath);

      gen.Emit(OpCodes.Call, ghScriptCmdConst); // call script command constructor with values loaded to stack
      gen.Emit(OpCodes.Nop);                    // add a few NOPs
      gen.Emit(OpCodes.Ret);                    // return

      return typeBuilder.CreateType();
    }
  }
}
