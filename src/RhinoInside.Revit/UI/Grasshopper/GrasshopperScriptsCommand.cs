using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using System.Windows.Forms;
using System.Windows.Forms.Interop;
using System.Reflection;
using System.Reflection.Emit;
using Autodesk.Revit.Attributes;
using DB = Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.PlugIns;
using RhinoInside.Revit.Settings;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class GrasshopperScriptsCommand : Command
  {
    public static void CreateUI(UIApplication uiApp)
    {
      foreach (var location in AddinOptions.ScriptLocations)
      {
        var scriptTypes = new Dictionary<string, Type>();

        // generate assembly containing script command types
        var assmName = $"GrasshopperScriptCommands{Guid.NewGuid()}";
        var assmFileName = $"{assmName}.dll";
        var assmPath = Path.GetTempPath();
        var assmInfo = GenerateGrasshopperScriptCommandAssembly(assmName, assmFileName, assmPath);
        var assmLoc = Path.Combine(assmPath, assmFileName);
        ProcessScriptsRecursive(location, (name, path) =>
        {
          scriptTypes[name] = GenerateGrasshopperScriptCommandType(assmInfo.Item2, name, path);
        });
        assmInfo.Item1.Save(assmFileName);
        Assembly.LoadFrom(assmLoc);

        var ribbonPanel = uiApp.CreateRibbonPanel(Addin.AddinName, Path.GetFileName(location));
        ProcessScriptsRecursive(location, (name, path) =>
        {
          try
          {
            var buttonData = NewScriptButton(
              scriptTypes[name],
              name,
              "",
              assmLoc
            );

            if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
            {
            }
          }
          catch (Exception ex)
          {
            TaskDialog.Show("GH", $"{scriptTypes[name].Assembly.Location}, {scriptTypes[name].FullName} | {ex.Message}");
          }
        });
      }
    }

    internal static void ProcessScriptsRecursive(string location, Action<string, string> processAction)
    {
      foreach (var subDir in Directory.GetDirectories(location))
        ProcessScriptsRecursive(subDir, processAction);

      foreach (var entry in Directory.GetFiles(location))
      {
        if (new string[] { ".gh", ".ghx" }.Contains(Path.GetExtension(entry).ToLower()))
        {
          string scriptName = Path.GetFileNameWithoutExtension(entry);
          processAction(scriptName, entry);
        }
      }
    }

    internal static PushButtonData NewScriptButton(Type scriptCmdType, string name, string tooltip, string assmLoc)
    {
      var commandName = scriptCmdType.Name + (name ?? "");
      var commandButtonName = name ?? commandName;
      var typeAssmLocation = assmLoc;
      var typeName = scriptCmdType.FullName;
      return new PushButtonData(commandName, commandButtonName, typeAssmLocation, typeName)
      {
        Image = ImageBuilder.LoadRibbonButtonImage("Resources.Ribbon.Grasshopper.GhFile.png", true),
        LargeImage = ImageBuilder.LoadRibbonButtonImage("Resources.Ribbon.Grasshopper.GhFile.png"),
        ToolTip = tooltip,
      };
    }

    internal static Type GenerateGrasshopperScriptCommandType(ModuleBuilder moduleBuilder, string scriptName, string scriptPath)
    {
      var typeBuilder = moduleBuilder.DefineType(
        scriptName,
        TypeAttributes.Public | TypeAttributes.Class,
        typeof(GrasshopperScriptCommand)
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
      var ghScriptCmdConst = typeof(GrasshopperScriptCommand).GetConstructor(new Type[] { typeof(string) });
      // define a base contructor
      var baseConst = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { });
      var gen = baseConst.GetILGenerator();
      gen.Emit(OpCodes.Ldarg_0);                // load "this" onto stack
      gen.Emit(OpCodes.Ldstr, scriptPath);      // load "scriptPath"
      gen.Emit(OpCodes.Call, ghScriptCmdConst); // call script command constructor with values loaded to stack
      gen.Emit(OpCodes.Nop);                    // add a few NOPs
      gen.Emit(OpCodes.Ret);                    // return

      return typeBuilder.CreateType();
    }

    internal static Tuple<AssemblyBuilder, ModuleBuilder> GenerateGrasshopperScriptCommandAssembly(string assemblyName, string assemblyFileName, string assemblyPath)
    {
      var asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
        new AssemblyName
        {
          Name = assemblyName,
          Version = new Version(0, 1)
        },
        AssemblyBuilderAccess.RunAndSave,
        assemblyPath
        );
      return new Tuple<AssemblyBuilder, ModuleBuilder>(
        asmBuilder,
        asmBuilder.DefineDynamicModule(assemblyName, assemblyFileName)
        );
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements) => throw new NotImplementedException();
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  public abstract class GrasshopperScriptCommand : Command
  {
    public class ScriptExecConfigs
    {
      public string ScriptPath;
    }

    public ScriptExecConfigs ExecCfgs;

    public GrasshopperScriptCommand(string scriptPath)
    {
      ExecCfgs = new ScriptExecConfigs
      {
        ScriptPath = scriptPath
      };
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      // run definition with grasshopper player
      return CommandGrasshopperPlayer.Execute(
        data.Application,
        data.Application.ActiveUIDocument?.ActiveView,
        new Dictionary<string, string>(),
        ExecCfgs.ScriptPath,
        ref message
        );
    }
  }
}
