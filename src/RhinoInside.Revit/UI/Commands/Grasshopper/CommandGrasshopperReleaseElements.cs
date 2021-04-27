using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperReleaseElements : GrasshopperCommand
  {
    public static string CommandName => "Release\nElements";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      // Create a push button to trigger a command add it to the ribbon panel.
      var buttonData = NewPushButtonData<CommandGrasshopperReleaseElements, AvailableWhenGHReady>
      (
        name: CommandName,
        iconName: "Ribbon.Grasshopper.ReleaseElements.png",
        tooltip: "Release elements created and tracked by Grasshopper",
        url: "guides/rir-grasshopper#element-tracking"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton)
      {
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      // TODO: kike ass magic here please :P
      return Result.Succeeded;
    }
  }
}
