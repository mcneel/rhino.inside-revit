using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.AddIn.Commands
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperCaptureElements : GrasshopperCommand
  {
    public static string CommandName => "Capture\nElements";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      // Create a push button to trigger a command add it to the ribbon panel.
      var buttonData = NewPushButtonData<CommandGrasshopperCaptureElements, NeedsActiveDocument<Availability>>
      (
        name: CommandName,
        iconName: "Ribbon.Grasshopper.CaptureElements.png",
        tooltip: "Capture elements from Revit model and bind to Grasshopper component",
        url: "guides/rir-grasshopper#element-tracking"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton)
      {
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      // TODO: kike add logic here please :D 
      return Result.Succeeded;
    }
  }
}
