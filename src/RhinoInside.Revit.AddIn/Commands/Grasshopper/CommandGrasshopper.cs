using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.AddIn.Commands
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopper : GrasshopperCommand
  {
    public static string CommandName => "Grasshopper";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      // Create a push button to trigger a command add it to the ribbon panel.
      var buttonData = NewPushButtonData<CommandGrasshopper, Availability>
      (
        name: CommandName,
        iconName: "Grasshopper.png",
        tooltip: "Shows Grasshopper window",
        url: "https://www.grasshopper3d.com/"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      // Check to see if any document path is provided in journal data
      // if yes, open the document.
      if (data.JournalData.TryGetValue("Open", out var filename))
      {
        if (!GH.Guest.OpenDocument(filename))
          return Result.Failed;
      }

      GH.Guest.ShowEditorAsync();

      return Result.Succeeded;
    }
  }
}
