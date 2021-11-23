using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Grasshopper;
using Grasshopper.Kernel;
using Microsoft.Win32.SafeHandles;

namespace RhinoInside.Revit.AddIn.Commands
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperRecompute : GrasshopperCommand
  {
    public static string CommandName => "Recompute";

    /// <summary>
    /// Available when Grasshopper canvas has a document loaded.
    /// </summary>
    protected class AvailableWhenCanvasHasDocument : Availability
    {
      protected override bool IsCommandAvailable(UIApplication app, CategorySet selectedCategories) =>
        base.IsCommandAvailable(app, selectedCategories) &&
        Instances.ActiveCanvas?.Document is object;
    }

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      // Create a push button to trigger a command add it to the ribbon panel.
      var buttonData = NewPushButtonData<CommandGrasshopperRecompute, AvailableWhenCanvasHasDocument>
      (
        name: CommandName,
        iconName: "Ribbon.Grasshopper.Recompute.png",
        tooltip: "Force a complete recompute of all objects"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        StoreButton(CommandName, pushButton);
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      if (Instances.ActiveCanvas?.Document is GH_Document definition)
      {
        if (GH_Document.EnableSolutions) definition.NewSolution(true);
        else
        {
          GH_Document.EnableSolutions = true;
          try { definition.NewSolution(false); }
          finally { GH_Document.EnableSolutions = false; }
        }

        // If there are no scheduled solutions return control back to Revit now
        if (definition.ScheduleDelay > GH_Document.ScheduleRecursive)
          WindowHandle.ActiveWindow = Rhinoceros.MainWindow;

        if (definition.SolutionState == GH_ProcessStep.PostProcess)
          return Result.Succeeded;
        else
          return Result.Cancelled;
      }

      return Result.Failed;
    }
  }
}
