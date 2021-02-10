using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.IO;

namespace RhinoInside.Revit.UI
{
  abstract class HelpCommand : Command
  {
    internal static void CreateUI(RibbonPanel ribbonPanel)
    {
      ribbonPanel.AddStackedItems(
        NewPushButtonData<CommandAPIDocs, AlwaysAvailable>(
          name: "APIDocs",
          iconName: "Resources.Link-icon.png",
          tooltip: "Opens apidocs.co website"
        ),
        NewPushButtonData<CommandTheBuildingCoder, AlwaysAvailable>(
          name: "TheBuildingCoder",
          iconName: "Resources.Link-icon.png",
          tooltip: "Opens thebuildingcoder.typepad.com website"
        ),
        NewPushButtonData<CommandRhinoDevDocs, AlwaysAvailable>(
          name: "Rhino Dev Docs",
          iconName: "Resources.Link-icon.png",
          tooltip: "Opens developer.rhino3d.com website"
        )
      );
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandAPIDocs : HelpCommand
  {
    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      using (System.Diagnostics.Process.Start(@"https://apidocs.co/apps/revit/" + $"{Revit.ActiveDBApplication?.VersionNumber}#")) { }

      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandTheBuildingCoder : HelpCommand
  {
    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      using (System.Diagnostics.Process.Start(@"https://thebuildingcoder.typepad.com/")) { }

      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandRhinoDevDocs : HelpCommand
  {
    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      using (System.Diagnostics.Process.Start(@"https://developer.rhino3d.com/")) { }

      return Result.Succeeded;
    }
  }
}
