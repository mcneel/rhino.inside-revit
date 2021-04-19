using System;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Rhino.PlugIns;

namespace RhinoInside.Revit.UI
{
  /// <summary>
  /// Base class for all Rhino.Inside Revit commands that call IronPython API
  /// </summary>
  public abstract class IronPyhtonCommand : RhinoCommand
  {
    protected static readonly Guid PluginId = new Guid("814d908a-e25c-493d-97e9-ee3861957f49");
    public IronPyhtonCommand()
    {
      if (!PlugIn.LoadPlugIn(PluginId, true, true))
        throw new Exception("Failed to startup IronPyhton");
    }

    /// <summary>
    /// Available when IronPython Plugin is available in Rhino
    /// </summary>
    protected new class Availability : RhinoCommand.AvailableWhenRhinoReady
    {
      public override bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
      {
        return base.IsCommandAvailable(applicationData, selectedCategories) &&
              (PlugIn.PlugInExists(PluginId, out bool loaded, out bool loadProtected) & (loaded | !loadProtected));
      }
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandPython : IronPyhtonCommand
  {
    public static string CommandName => "Python\nEditor";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      var buttonData = NewPushButtonData<CommandPython, AvailableWhenRhinoReady>
      (
        name: CommandName,
        iconName: "Python.png",
        tooltip: "Shows Python editor window",
        url: "https://developer.rhino3d.com/guides/rhinopython/"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        pushButton.LongDescription = $"Use CTRL key to open only Python editor window without restoring other tool windows";
        StoreButton(CommandName, pushButton);
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      Rhinoceros.RunScriptAsync("_EditPythonScript", activate: true);
      return Result.Succeeded;
    }
  }
}
