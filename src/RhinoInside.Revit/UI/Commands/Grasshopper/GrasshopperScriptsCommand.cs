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
    public Type ScriptCommandType;
  }

  abstract class GrasshopperScriptsCommand : Command
  {
    public static void CreateUI(Func<string, RibbonPanel> panelMaker)
    {
      foreach (var location in AddinOptions.Current.ScriptLocations)
      {
        // --------------------------------------------------------------------
        // FIND SCRIPTS
        // --------------------------------------------------------------------
        var items = FindLinkedItemsRecursive(location);

        // --------------------------------------------------------------------
        // CREATE ASSEMBLY
        // --------------------------------------------------------------------
        // generate assembly containing script command types
        var assmInfo = new LinkedScriptAssemblyInfo();

        // create types for all the scripts in the structure
        ProcessLinkedScripts(items, (script) =>
        {
          script.ScriptCommandType = assmInfo.MakeScriptCommandType(script);
        });

        // save and load the created assembly
        assmInfo.SaveAndLoad();

        // --------------------------------------------------------------------
        // CREATE UI
        // --------------------------------------------------------------------
        var panel = panelMaker(Path.GetFileName(location));

        // Currently only supporting two levels in the UI:
        // 1) Pushbuttons on panel for every LinkedScript at the root level
        // 2) Pulldowns containing pushbuttons for all the LinkedScripts recursively found under their directory
        // Lets make the pulldowns first so they are first on the panel
        items.OfType<LinkedItemGroup>().ToList().ForEach((group) =>
        {
          var pullDownData = new PulldownButtonData(group.Name, group.Name)
          {
            Image = ImageBuilder.LoadRibbonButtonImage("Ribbon.Grasshopper.GhFolder.png", true),
            LargeImage = ImageBuilder.LoadRibbonButtonImage("Ribbon.Grasshopper.GhFolder.png"),
            ToolTip = group.Tooltip,
          };
          if (panel.AddItem(pullDownData) is PulldownButton pulldown)
          {
            ProcessLinkedScripts(group.Items, (script) =>
            {
              AddPullDownButton(pulldown, script, assmInfo);
            });
          }
        });
        // now make pushbuttons
        items.OfType<LinkedScript>().ToList().ForEach((script) =>
        {
          AddPanelButton(panel, script, assmInfo);
        });
      }
    }

    internal static List<LinkedItem> FindLinkedItemsRecursive(string location)
    {
      var items = new List<LinkedItem>();

      foreach (var subDir in Directory.GetDirectories(location))
      {
        // only go one level deep
        items.Add(
          new LinkedItemGroup
          {
            Name = Path.GetFileName(subDir),
            Items = FindLinkedItemsRecursive(subDir),
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

      return items.OrderBy(x => x.Name).ToList();
    }

    internal static void ProcessLinkedScripts(List<LinkedItem> items, Action<LinkedScript> action)
    {
      items.ForEach((item) =>
      {
        switch (item)
        {
          case LinkedItemGroup group: ProcessLinkedScripts(group.Items, action); break;
          case LinkedScript script: action(script); break;
        }
      });
    }

    internal static void AddPullDownButton(PulldownButton pulldown, LinkedScript script, LinkedScriptAssemblyInfo assmInfo)
    {
      if (pulldown.AddPushButton(NewScriptButton(script, assmInfo.FilePath)) is PushButton pushButton)
      {
        // do stuff with button?
      }
    }

    internal static void AddPanelButton(RibbonPanel panel, LinkedScript script, LinkedScriptAssemblyInfo assmInfo)
    {
      if (panel.AddItem(NewScriptButton(script, assmInfo.FilePath)) is PushButton pushButton)
      {
        // do stuff with button?
      }
    }

    internal static PushButtonData NewScriptButton(LinkedScript script, string assmLoc)
    {
      var commandName = script.ScriptCommandType.Name + (script.Name ?? "");
      var commandButtonName = script.Name ?? commandName;
      var typeAssmLocation = assmLoc;
      var typeName = script.ScriptCommandType.FullName;
      return new PushButtonData(commandName, commandButtonName, typeAssmLocation, typeName)
      {
        Image = ImageBuilder.LoadRibbonButtonImage($"Ribbon.Grasshopper.{script.ScriptType}.png", true),
        LargeImage = ImageBuilder.LoadRibbonButtonImage($"Ribbon.Grasshopper.{script.ScriptType}.png"),
        ToolTip = "Launch script in Grasshopper player",
        LongDescription = $"Script Path: {script.ScriptPath}",
      };
    }

    public class LinkedScriptAssemblyInfo
    {
      public LinkedScriptAssemblyInfo()
      {
        Name = $"LinkedScriptAssm-{Guid.NewGuid()}";
        FileName = $"{Name}.dll";
        FileLocation = Path.GetTempPath();

        AssmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
          new AssemblyName
          {
            Name = Name,
            Version = new Version(0, 1)
          },
          AssemblyBuilderAccess.RunAndSave,
          FileLocation
          );

        ModuleBuilder = AssmBuilder.DefineDynamicModule(Name, FileName);
      }

      public string Name { get; private set; }
      public string FileLocation { get; private set; }
      public string FileName { get; private set; }
      public string FilePath => Path.Combine(FileLocation, FileName);

      public AssemblyBuilder AssmBuilder { get; private set; }
      public ModuleBuilder ModuleBuilder { get; private set; }

      public void SaveAndLoad()
      {
        AssmBuilder?.Save(FileName);
        Assembly.LoadFrom(FilePath);
      }

      public Type MakeScriptCommandType(LinkedScript script)
      {
        var typeBuilder = ModuleBuilder.DefineType(
          $"LinkedScriptCmd-{Guid.NewGuid()}",
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
