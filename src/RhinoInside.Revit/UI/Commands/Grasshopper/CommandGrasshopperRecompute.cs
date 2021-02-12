using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Grasshopper;
using Grasshopper.Kernel;
using Microsoft.Win32.SafeHandles;
using Rhino.PlugIns;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.GH.Bake;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperRecompute : GrasshopperCommand
  {
    public static string CommandName => "Recompute";

    protected new class Availability : GrasshopperCommand.Availability
    {
      public override bool IsCommandAvailable(UIApplication _, DB.CategorySet selectedCategories) =>
        base.IsCommandAvailable(_, selectedCategories) &&
        Instances.ActiveCanvas?.Document is object;
    }

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      // Create a push button to trigger a command add it to the ribbon panel.
      var buttonData = NewPushButtonData<CommandGrasshopperRecompute, Availability>(
        CommandName,
        "Ribbon.Grasshopper.Recompute.png",
        "Force a complete recompute of all objects"
        );
      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        pushButton.Visible = PlugIn.PlugInExists(PluginId, out bool _, out bool _);
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
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
