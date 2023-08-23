using System;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Rhino.PlugIns;

namespace RhinoInside.Revit.AddIn.Commands
{
  /// <summary>
  /// Base class for all Rhino.Inside Revit commands that call RhinoCode API
  /// </summary>
  public abstract class RhinoCodeCommand : RhinoCommand
  {
    protected static readonly Guid PlugInId = new Guid("C9CBA87A-23CE-4F15-A918-97645C05CDE7");
    public RhinoCodeCommand()
    {
      if (!PlugIn.LoadPlugIn(PlugInId, true, true))
        throw new Exception("Failed to startup RhinoCode");
    }

    /// <summary>
    /// Available when IronPython Plugin is available in Rhino.
    /// </summary>
    protected new class Availability : RhinoCommand.Availability
    {
      protected override bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories) =>
        base.IsCommandAvailable(applicationData, selectedCategories) &&
        (PlugIn.PlugInExists(PlugInId, out bool loaded, out bool loadProtected) & (loaded | !loadProtected));
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandScriptEditor : RhinoCodeCommand
  {
    public CommandScriptEditor()
    {
      if (!PlugIn.LoadPlugIn(PlugInId, true, true))
        throw new Exception("Failed to startup Script Editor");
    }

    public static string CommandName => "Script\nEditor";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
#if RHINO_8
      var buttonData = NewPushButtonData<CommandScriptEditor, Availability>
      (
        name: CommandName,
        iconName: "ScriptEditor.png",
        tooltip: "Shows script editor window",
        url: "https://developer.rhino3d.com/"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        StoreButton(CommandName, pushButton);
      }
#endif
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      Rhinoceros.RunScriptAsync("!_ScriptEditor", activate: true);
      return Result.Succeeded;
    }
  }
}
