using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.UI
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
      using (System.Diagnostics.Process.Start(@"https://www.rhino3d.com/inside/revit/beta/guides/")) { }
      return Result.Succeeded;
    }
  }
}
