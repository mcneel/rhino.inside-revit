using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandRhinoOptions : RhinoCommand
  {
    public static string CommandName => "Rhino Options";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      var buttonData = NewPushButtonData<CommandRhinoOptions, Availability>
      (
        name: CommandName,
        iconName: "Rhino.png",
        tooltip: "Shows Rhino Options window"
      );

      // set this button as the panel dialog-launcher (arrow-button at the corner of panel)
      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
        ribbonPanel.SetDialogLauncherButton(AddIn.AddinName, pushButton);
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      return Rhinoceros.RunCommandOptions();
    }
  }
}
