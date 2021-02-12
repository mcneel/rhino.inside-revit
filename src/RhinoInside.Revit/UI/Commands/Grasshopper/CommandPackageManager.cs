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
  class CommandGrasshopperPackageManager : GrasshopperCommand
  {
    public static string CommandName => "Package\nManager";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      // Create a push button to trigger a command add it to the ribbon panel.
      var buttonData = NewPushButtonData<CommandGrasshopperPackageManager, Availability>(
        CommandName,
        "PackageManager-icon.png",
        "Shows Rhino/Grasshopper Package Manager"
      );
      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        pushButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://www.food4rhino.com/"));
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      Rhinoceros.RunCommandPackageManager();
      return Result.Succeeded;
    }
  }
}
