using System;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Rhino.PlugIns;

namespace RhinoInside.Revit.AddIn.Commands
{
  /// <summary>
  /// Base class for all Rhino.Inside Revit commands that call IronPython API
  /// </summary>
  public abstract class IronPyhtonCommand : RhinoCommand
  {
    protected static readonly Guid PlugInId = new Guid("814d908a-e25c-493d-97e9-ee3861957f49");
    static bool _Loaded = false;
    public IronPyhtonCommand()
    {
      if (!_Loaded && !PlugIn.LoadPlugIn(PlugInId, true, true))
        throw new Exception("Failed to startup IronPyhton");

      _Loaded = true;
    }

    /// <summary>
    /// Available when IronPython Plugin is available in Rhino.
    /// </summary>
    protected new class Availability : RhinoCommand.Availability
    {
      protected override bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories) =>
        base.IsCommandAvailable(applicationData, selectedCategories) &&
        (_Loaded || (PlugIn.PlugInExists(PlugInId, out bool loaded, out bool loadProtected) & (loaded | !loadProtected)));
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandPython : IronPyhtonCommand
  {
    public static string CommandName => "Python\nEditor";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
#if !RHINO_8
      var buttonData = NewPushButtonData<CommandPython, Availability>
      (
        name: CommandName,
        iconName: "Ribbon.Rhinoceros.Python.png",
        tooltip: "Shows Python editor window",
        url: "https://developer.rhino3d.com/guides/rhinopython/"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
      }
#endif
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      Rhinoceros.RunScriptAsync("!_EditPythonScript", activate: true);
      return Result.Succeeded;
    }
  }
}
