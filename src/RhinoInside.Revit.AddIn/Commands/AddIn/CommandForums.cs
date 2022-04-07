using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.AddIn.Commands
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandForums : Command
  {
    public static string CommandName = "Forums";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      var buttonData = NewPushButtonData<CommandForums, AlwaysAvailable>
      (
        name: CommandName,
        iconName: "Forum-icon.png",
        tooltip: "Opens discourse.mcneel.com website",
        url: "reference/rir-interface#more-slideout"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      using (System.Diagnostics.Process.Start(@"https://discourse.mcneel.com/c/rhino-inside/revit")) { }
      return Result.Succeeded;
    }
  }
}
