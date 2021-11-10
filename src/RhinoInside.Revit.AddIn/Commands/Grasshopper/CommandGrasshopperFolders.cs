using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.AddIn.Commands
{
  abstract class CommandGrasshopperFolders : GrasshopperCommand
  {
    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      ribbonPanel.AddStackedItems
      (
        NewPushButtonData<CommandGrasshopperUserObjectsFolder, Availability>
        (
          name: CommandGrasshopperUserObjectsFolder.CommandName,
          iconName: "Ribbon.Grasshopper.GHSpecialFolder.png",
          tooltip: "Shows Grasshopper UserObjects Folder"
        ),
        NewPushButtonData<CommandGrasshopperClustersFolder, Availability>
        (
          name: CommandGrasshopperClustersFolder.CommandName,
          iconName: "Ribbon.Grasshopper.GHSpecialFolder.png",
          tooltip: "Shows Grasshopper Clusters Folder"
        ),
        NewPushButtonData<CommandGrasshopperComponentsFolder, Availability>
        (
          name: CommandGrasshopperComponentsFolder.CommandName,
          iconName: "Ribbon.Grasshopper.GHSpecialFolder.png",
          tooltip: "Shows Grasshopper Components Folder"
        )
      );
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperUserObjectsFolder : GrasshopperCommand
  {
    public static string CommandName => "UserObjects Folder";

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      Grasshopper.Folders.ShowFolderInExplorer(Grasshopper.Folders.DefaultUserObjectFolder);
      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperClustersFolder : GrasshopperCommand
  {
    public static string CommandName => "Clusters Folder";

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      Grasshopper.Folders.ShowFolderInExplorer(Grasshopper.Folders.DefaultClusterFolder);
      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperComponentsFolder : GrasshopperCommand
  {
    public static string CommandName => "Components Folder";

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      Grasshopper.Folders.ShowFolderInExplorer(Grasshopper.Folders.DefaultAssemblyFolder);
      return Result.Succeeded;
    }
  }
}
