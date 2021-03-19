using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandForums : Command
  {
    public static string CommandName = "Forums";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      var buttonData = NewPushButtonData<CommandForums, AlwaysAvailable>(CommandName, "Forum-icon.png", "");
      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        pushButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, @"https://www.rhino3d.com/inside/revit/beta/"));
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      using (System.Diagnostics.Process.Start(@"https://discourse.mcneel.com/c/rhino-inside/Revit")) { }
      return Result.Succeeded;
    }
  }

}
