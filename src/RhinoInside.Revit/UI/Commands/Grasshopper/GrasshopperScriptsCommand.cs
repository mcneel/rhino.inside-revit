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
  /// <summary>
  /// Types of scripts that can be executed
  /// </summary>
  public enum ScriptType
  {
    GhFile = 0,
    GhxFile,
  }

  /// <summary>
  /// Generic linked item
  /// </summary>
  abstract class LinkedItem
  {
    public string Name;
    public string Tooltip = string.Empty;
  }

  /// <summary>
  /// Group of linked items
  /// </summary>
  class LinkedItemGroup : LinkedItem
  {
    public string GroupPath;
    public List<LinkedItem> Items = new List<LinkedItem>();
  }

  /// <summary>
  /// Linked script
  /// </summary>
  class LinkedScript : LinkedItem
  {
    public ScriptType ScriptType;
    public string ScriptPath;
  }

  abstract class GrasshopperScriptsCommand : Command
  {
    // TODO:
    // - calculate hash?
    // - cleanup temp
    // - implement cleaning up panel buttons to create new ones
    // - implement watcher
    public static void CreateUI(UIApplication uiApp)
    {
      foreach (var location in AddinOptions.ScriptLocations)
      {
        // --------------------------------------------------------------------
        // FIND SCRIPTS
        // --------------------------------------------------------------------
        var items = FindScriptsRecursive(location);

        // --------------------------------------------------------------------
        // CREATE ASSEMBLY
        // --------------------------------------------------------------------
        // generate assembly containing script command types
        var assmName = $"GrasshopperScriptCommands{Guid.NewGuid()}";
        var assmFileName = $"{assmName}.dll";
        var assmPath = Path.GetTempPath();
        var assmInfo = GenerateGrasshopperScriptCommandAssembly(assmName, assmFileName, assmPath);
        var assmLoc = Path.Combine(assmPath, assmFileName);
        var scriptTypes = new Dictionary<string, Type>();
        // not need to recurse over data since we are only going one level deep
        // groups are turned into PullDowns, scripts are turned into PushButons
        // But first lets create command types for all scripts
        // groups don't need a type since they don't execute anything
        items.OfType<LinkedItemGroup>().ToList().ForEach((group) =>
        {
          group.Items.OfType<LinkedScript>().ToList().ForEach((script) =>
          {
            scriptTypes[script.Name] = GenerateGrasshopperScriptCommandType(assmInfo.Item2, script.ScriptType, script.Name, script.ScriptPath);
          });
        });
        items.OfType<LinkedScript>().ToList().ForEach((script) =>
        {
          scriptTypes[script.Name] = GenerateGrasshopperScriptCommandType(assmInfo.Item2, script.ScriptType, script.Name, script.ScriptPath);
        });

        // save and load the created assembly
        assmInfo.Item1.Save(assmFileName);
        Assembly.LoadFrom(assmLoc);

        // --------------------------------------------------------------------
        // CREATE UI
        // --------------------------------------------------------------------
        var ribbonPanel = uiApp.CreateRibbonPanel(Addin.AddinName, Path.GetFileName(location));

        // create pulldown buttons and their sub pushbuttons
        items.OfType<LinkedItemGroup>().ToList().ForEach((group) =>
        {
          var pullDownData = new PulldownButtonData(group.Name, group.Name)
          {
            Image = ImageBuilder.LoadRibbonButtonImage("Ribbon.Grasshopper.GhFolder.png", true),
            LargeImage = ImageBuilder.LoadRibbonButtonImage("Ribbon.Grasshopper.GhFolder.png"),
            ToolTip = group.Tooltip,
          };
          if (ribbonPanel.AddItem(pullDownData) is PulldownButton pulldown)
          {
            group.Items.OfType<LinkedScript>().ToList().ForEach((script) =>
            {
              var buttonData = NewScriptButton(
                scriptCmdType: scriptTypes[script.Name],
                script: script,
                assmLoc: assmLoc
              );
              if (pulldown.AddPushButton(buttonData) is PushButton pushButton)
              {
                // do stuff with button?
              }
            });
          }
        });

        // create push buttons on the panel
        items.OfType<LinkedScript>().ToList().ForEach((script) =>
        {
          var buttonData = NewScriptButton(
                scriptCmdType: scriptTypes[script.Name],
                script: script,
                assmLoc: assmLoc
              );
          if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
          {
            // do stuff with button?
          }
        });
      }
    }

    internal static List<LinkedItem> FindScriptsRecursive(string location)
    {
      var items = new List<LinkedItem>();

      foreach (var subDir in Directory.GetDirectories(location))
      {
        // only go one level deep
        items.Add(
          new LinkedItemGroup
          {
            Name = Path.GetFileName(subDir),
            Items = FindScriptsRecursive(subDir),
          }
        );
      }

      foreach (var entry in Directory.GetFiles(location))
      {
        var ext = Path.GetExtension(entry).ToLower();
        if (new string[] { ".gh", ".ghx" }.Contains(ext))
        {
          items.Add(
            new LinkedScript
            {
              ScriptType = ext == ".gh" ? ScriptType.GhFile : ScriptType.GhxFile,
              ScriptPath = entry,
              Name = Path.GetFileNameWithoutExtension(entry),
            }
          );
        }
      }

      return items;
    }

    internal static PushButtonData NewScriptButton(Type scriptCmdType, LinkedScript script, string assmLoc)
    {
      var commandName = scriptCmdType.Name + (script.Name ?? "");
      var commandButtonName = script.Name ?? commandName;
      var typeAssmLocation = assmLoc;
      var typeName = scriptCmdType.FullName;
      return new PushButtonData(commandName, commandButtonName, typeAssmLocation, typeName)
      {
        Image = ImageBuilder.LoadRibbonButtonImage($"Ribbon.Grasshopper.{script.ScriptType}.png", true),
        LargeImage = ImageBuilder.LoadRibbonButtonImage($"Ribbon.Grasshopper.{script.ScriptType}.png"),
        ToolTip = "Launch script in Grasshopper player",
        LongDescription = $"Script Path: {script.ScriptPath}",
      };
    }

    internal static Type GenerateGrasshopperScriptCommandType(ModuleBuilder moduleBuilder, ScriptType scriptType, string scriptName, string scriptPath)
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
      var ghScriptCmdConst = typeof(GrasshopperScriptCommand).GetConstructor(new Type[] {
        typeof(int),        // "scriptType"
        typeof(string)      // "scriptPath"
      });
      // define a base contructor
      var baseConst = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { });
      var gen = baseConst.GetILGenerator();
      gen.Emit(OpCodes.Ldarg_0);                // load "this" onto stack
                                                // load "scriptType"
      gen.Emit(OpCodes.Ldc_I4, (int)scriptType);
                                                // load "scriptPath"
      gen.Emit(OpCodes.Ldstr, scriptPath);

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
  }

  /// <summary>
  /// Base class for all the linked-script buttons in the UI. This class is dyanmically copied,
  /// extended, and configured to point to the script file and then is tied to the button on the UI
  /// </summary>
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  public abstract class GrasshopperScriptCommand : Command
  {
    /* Note:
     * There is no base constructor. It is generated dyanmically when copying this base type
     */

    /// <summary>
    /// Configurations for the target script
    /// </summary>
    public class ScriptExecConfigs
    {
      public ScriptType ScriptType = ScriptType.GhFile;
      public string ScriptPath;
    }

    /// <summary>
    /// Script configurations for this instance
    /// </summary>
    public ScriptExecConfigs ExecCfgs;

    /// <summary>
    /// Create new instance pointing to given script
    /// </summary>
    /// <param name="scriptPath">Full path of script file</param>
    public GrasshopperScriptCommand(int scriptType, string scriptPath)
    {
      ExecCfgs = new ScriptExecConfigs
      {
        ScriptType = (ScriptType) scriptType,
        ScriptPath = scriptPath
      };
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      switch(ExecCfgs.ScriptType)
      {
        case ScriptType.GhFile:
        case ScriptType.GhxFile:
          return ExecuteGH(data, ref message);

        default: return Result.Succeeded;
      }
    }

    private Result ExecuteGH(ExternalCommandData data, ref string message)
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
