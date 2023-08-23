using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.AddIn.Commands
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandPackageManager : RhinoCommand
  {
    public static string CommandName => "Package\nManager";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      // Create a push button to trigger a command add it to the ribbon panel.
      var buttonData = NewPushButtonData<CommandPackageManager, Availability>
      (
        name: CommandName,
        iconName: "Ribbon.Rhinoceros.PackageManager.png",
        tooltip: "Shows Rhino Package Manager",
        url: "https://www.food4rhino.com/"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton)
      {
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      return Rhinoceros.RunCommandPackageManager();
    }
  }
}
