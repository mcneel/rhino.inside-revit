using System;
using System.Windows.Input;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Rhino.PlugIns;

namespace RhinoInside.Revit.AddIn.Commands
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandRhino : RhinoCommand
  {
    public static string CommandName => "Rhino";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      var buttonData = NewPushButtonData<CommandRhino, Availability>
      (
        name: CommandName,
        iconName: "Rhino.png",
        tooltip: "Shows Rhino window",
        url: "reference/rir-interface#rhinoceros-panel"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        pushButton.LongDescription = $"Use CTRL key to open a Rhino model";
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        Rhinoceros.RunCommandAbout();
      else
        Rhinoceros.ShowAsync();

      return Result.Succeeded;
    }
  }

  /// <summary>
  /// Base class for all Rhino.Inside Revit commands that are implemented into Commands.rhp
  /// </summary>
  public abstract class RhinoBuiltInCommand : RhinoCommand
  {
    protected static readonly Guid PlugInId = new Guid("02BF604D-799C-4CC2-830E-8D72F21B14B7");
    static bool _Loaded = false;
    protected RhinoBuiltInCommand()
    {
      if (!_Loaded && !PlugIn.LoadPlugIn(PlugInId, true, true))
        throw new Exception("Failed to startup Commands");

      _Loaded = true;
    }

    /// <summary>
    /// Available when Commands Plugin is available in Rhino.
    /// </summary>
    protected new class Availability : RhinoCommand.Availability
    {
      protected override bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories) =>
        base.IsCommandAvailable(applicationData, selectedCategories) &&
        (_Loaded || (PlugIn.PlugInExists(PlugInId, out bool loaded, out bool loadProtected) & (loaded | !loadProtected)));
    }
  }
}
