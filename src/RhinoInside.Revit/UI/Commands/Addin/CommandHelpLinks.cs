using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.UI
{
  abstract class CommandHelpLinks : Command
  {
    internal static void CreateUI(RibbonPanel ribbonPanel)
    {
      ribbonPanel.AddStackedItems(
        NewPushButtonData<CommandAPIDocs, AlwaysAvailable>(
          name: CommandAPIDocs.CommandName,
          iconName: "Link-icon.png",
          tooltip: "Opens apidocs.co (Revit API documentation) website"
        ),
        NewPushButtonData<CommandTheBuildingCoder, AlwaysAvailable>(
          name: CommandTheBuildingCoder.CommandName,
          iconName: "Link-icon.png",
          tooltip: "Opens TheBuildingCode website"
        ),
        NewPushButtonData<CommandRhinoDevDocs, AlwaysAvailable>(
          name: CommandRhinoDevDocs.CommandName,
          iconName: "Link-icon.png",
          tooltip: "Opens Rhino Developer documentation website"
        )
      );
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandAPIDocs : Command
  {
    public static string CommandName = "Revit API Docs";

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      using (System.Diagnostics.Process.Start(@"https://apidocs.co/apps/revit/" + $"{Revit.ActiveDBApplication?.VersionNumber}#")) { }
      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandTheBuildingCoder : Command
  {
    public static string CommandName = "TheBuildingCoder";

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      using (System.Diagnostics.Process.Start(@"https://thebuildingcoder.typepad.com/")) { }
      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandRhinoDevDocs : Command
  {
    public static string CommandName = "Rhino Dev Docs";

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      using (System.Diagnostics.Process.Start(@"https://developer.rhino3d.com/")) { }
      return Result.Succeeded;
    }
  }
}
