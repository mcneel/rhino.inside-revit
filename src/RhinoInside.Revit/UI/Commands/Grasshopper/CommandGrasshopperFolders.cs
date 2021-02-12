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
  abstract class CommandGrasshopperFolders : Command
  {
    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      ribbonPanel.AddStackedItems(
        NewPushButtonData<CommandGrasshopperUserObjectsFolder, AlwaysAvailable>(
          name: CommandGrasshopperUserObjectsFolder.CommandName,
          iconName: "Ribbon.Grasshopper.GHSpecialFolder.png",
          tooltip: "Shows Grasshopper UserObjects Folder"
        ),
        NewPushButtonData<CommandGrasshopperClustersFolder, AlwaysAvailable>(
          name: CommandGrasshopperClustersFolder.CommandName,
          iconName: "Ribbon.Grasshopper.GHSpecialFolder.png",
          tooltip: "Shows Grasshopper Clusters Folder"
        ),
        NewPushButtonData<CommandGrasshopperComponentsFolder, AlwaysAvailable>(
          name: CommandGrasshopperComponentsFolder.CommandName,
          iconName: "Ribbon.Grasshopper.GHSpecialFolder.png",
          tooltip: "Shows Grasshopper Components Folder"
        )
      );
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperUserObjectsFolder : CommandHelpLinks
  {
    public static string CommandName => "UserObjects Folder";

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      Rhinoceros.RunCommandGHFolder(option: "U");
      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperClustersFolder : CommandHelpLinks
  {
    public static string CommandName => "Clusters Folder";

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      Rhinoceros.RunCommandGHFolder(option: "C");
      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperComponentsFolder : CommandHelpLinks
  {
    public static string CommandName => "Components Folder";

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      Rhinoceros.RunCommandGHFolder(option: "o");
      return Result.Succeeded;
    }
  }
}
