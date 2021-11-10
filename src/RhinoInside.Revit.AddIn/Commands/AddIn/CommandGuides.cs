using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.AddIn.Commands
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGuides : Command
  {
    public static string CommandName = "Guides";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      var buttonData = NewPushButtonData<CommandGuides, AlwaysAvailable>
      (
        name: CommandName,
        iconName: "Guides-icon.png",
        tooltip: "Opens Rhino.Inside Revit guides page",
        url: "reference/rir-interface#more-slideout"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      using (System.Diagnostics.Process.Start($@"{Core.WebSite}/guides/")) { }
      return Result.Succeeded;
    }
  }
}
