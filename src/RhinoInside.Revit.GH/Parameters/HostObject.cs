using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class HostObject : GraphicalElementT<Types.IGH_HostObject, DB.HostObject>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("E3462915-3C4D-4864-9DD4-5A73F91C6543");

    public HostObject() : base("Host", "Host", "Contains a collection of host elements", "Params", "Revit Primitives") { }

    protected override Types.IGH_HostObject InstantiateT() => new Types.HostObject();
  }

  public class HostObjectType : Element<Types.IGH_HostObjectType, DB.HostObjAttributes>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("708AB072-878E-41ED-9B8C-AAB0E1D85A53");

    public HostObjectType() : base("Host Type", "HostType", "Contains a collection of Revit host types", "Params", "Revit Primitives") { }

    protected override Types.IGH_HostObjectType InstantiateT() => new Types.HostObjectType();
  }

  public class BuildingPad : GraphicalElementT<Types.BuildingPad, DB.Architecture.BuildingPad>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("0D0AFE5F-4578-493E-8374-C6BD1C5395BE");

    public BuildingPad() : base("Building Pad", "Building Pad", "Contains a collection of Revit building pad elements", "Params", "Revit Primitives") { }

    #region UI
    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var BuildingPad = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.BuildingPad);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(BuildingPad),
        Revit.ActiveUIApplication.CanPostCommand(BuildingPad),
        false
      );
    }
    #endregion
  }

  public class CurtainGridLine : GraphicalElementT<Types.CurtainGridLine, DB.CurtainGridLine>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("A2DD571E-729C-4F69-BD34-2769583D329B");

    public CurtainGridLine() : base("Curtain Grid Line", "Curtain Grid Line", "Contains a collection of Revit curtain grid line elements", "Params", "Revit Primitives") { }

    #region UI
    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var CurtainGrid = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.CurtainGrid);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(CurtainGrid),
        Revit.ActiveUIApplication.CanPostCommand(CurtainGrid),
        false
      );
    }
    #endregion
  }

  public class CurtainSystem : GraphicalElementT<Types.CurtainSystem, DB.CurtainSystem>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("E94B20E9-C2AA-4FC7-939D-ECD071EA45DA");

    public CurtainSystem() : base("Curtain System", "Curtain System", "Contains a collection of Revit curtain system elements", "Params", "Revit Primitives") { }

    #region UI
    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var CurtainSystemByFace = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.CurtainSystemByFace);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(CurtainSystemByFace),
        Revit.ActiveUIApplication.CanPostCommand(CurtainSystemByFace),
        false
      );
    }
    #endregion
  }

  public class Ceiling : GraphicalElementT<Types.Ceiling, DB.Ceiling>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("7FCEA93D-8CDE-446C-9167-E8B590342C66");

    public Ceiling() : base("Ceiling", "Ceiling", "Contains a collection of Revit ceiling elements", "Params", "Revit Primitives") { }
  }

  public class Floor : GraphicalElementT<Types.Floor, DB.Floor>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("45616DF6-59BF-4480-A133-8F9B3BA27AF1");

    public Floor() : base("Floor", "Floor", "Contains a collection of Revit floor elements", "Params", "Revit Primitives") { }

    #region UI
    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var create = Menu_AppendItem(menu, $"Set new {TypeName}");

      var ArchitecturalFloor = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.ArchitecturalFloor);
      Menu_AppendItem
      (
        create.DropDown, "Architectural",
        Menu_PromptNew(ArchitecturalFloor),
        Revit.ActiveUIApplication.CanPostCommand(ArchitecturalFloor),
        false
      );

      var StructuralFloor = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.StructuralFloor);
      Menu_AppendItem
      (
        create.DropDown, "Structural",
        Menu_PromptNew(StructuralFloor),
        Revit.ActiveUIApplication.CanPostCommand(StructuralFloor),
        false
      );

      var FloorByFaceFloor = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.FloorByFaceFloor);
      Menu_AppendItem
      (
        create.DropDown, "By Face",
        Menu_PromptNew(FloorByFaceFloor),
        Revit.ActiveUIApplication.CanPostCommand(FloorByFaceFloor),
        false
      );
    }
    #endregion
  }

  public class Roof : GraphicalElementT<Types.Roof, DB.RoofBase>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("D75E33E2-2508-42E6-AEF1-B05759A495AB");

    public Roof() : base("Roof", "Roof", "Contains a collection of Revit roof elements", "Params", "Revit Primitives") { }

    #region UI
    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var create = Menu_AppendItem(menu, $"Set new {TypeName}");

      var RoofByFootprint = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.RoofByFootprint);
      Menu_AppendItem
      (
        create.DropDown, "Footprint",
        Menu_PromptNew(RoofByFootprint),
        Revit.ActiveUIApplication.CanPostCommand(RoofByFootprint),
        false
      );

      var RoofByExtrusion = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.RoofByExtrusion);
      Menu_AppendItem
      (
        create.DropDown, "Extrusion",
        Menu_PromptNew(RoofByExtrusion),
        Revit.ActiveUIApplication.CanPostCommand(RoofByExtrusion),
        false
      );

      var RoofByFace = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.RoofByFace);
      Menu_AppendItem
      (
        create.DropDown, "By Face",
        Menu_PromptNew(RoofByFace),
        Revit.ActiveUIApplication.CanPostCommand(RoofByFace),
        false
      );
    }
    #endregion
  }

  public class Wall : GraphicalElementT<Types.Wall, DB.Wall>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("15AD6BF9-63AD-462B-985D-F6B8C2299465");

    public Wall() : base("Wall", "Wall", "Contains a collection of Revit wall elements", "Params", "Revit Primitives") { }

    #region UI
    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var create = Menu_AppendItem(menu, $"Set new {TypeName}");

      var ArchitecturalWallId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.ArchitecturalWall);
      Menu_AppendItem
      (
        create.DropDown, "Architectural",
        Menu_PromptNew(ArchitecturalWallId),
        Revit.ActiveUIApplication.CanPostCommand(ArchitecturalWallId),
        false
      );

      var StructuralWallId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.StructuralWall);
      Menu_AppendItem
      (
        create.DropDown, "Structural",
        Menu_PromptNew(StructuralWallId),
        Revit.ActiveUIApplication.CanPostCommand(StructuralWallId),
        false
      );

      var WallByFaceWall = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.WallByFaceWall);
      Menu_AppendItem
      (
        create.DropDown, "By Face",
        Menu_PromptNew(WallByFaceWall),
        Revit.ActiveUIApplication.CanPostCommand(WallByFaceWall),
        false
      );
    }
    #endregion
  }
}
