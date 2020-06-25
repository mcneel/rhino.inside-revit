using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class HostObject : GraphicalElementT<Types.HostObject, DB.HostObject>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("E3462915-3C4D-4864-9DD4-5A73F91C6543");

    public HostObject() : base("Host", "Host", "Represents a Revit document host element.", "Params", "Revit Primitives") { }
  }

  public class HostObjectType : ElementIdWithoutPreviewParam<Types.HostObjectType, DB.HostObjAttributes>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("708AB072-878E-41ED-9B8C-AAB0E1D85A53");

    public HostObjectType() : base("Host Type", "HostType", "Represents a Revit document host element type.", "Params", "Revit Primitives") { }

    protected override Types.HostObjectType PreferredCast(object data) => data is DB.HostObjAttributes type ? new Types.HostObjectType(type) : null;
  }

  public class BuildingPad : GraphicalElementT<Types.BuildingPad, DB.Architecture.BuildingPad>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("0D0AFE5F-4578-493E-8374-C6BD1C5395BE");

    public BuildingPad() : base("Building Pad", "Building Pad", "Represents a Revit document building pad element.", "Params", "Revit Primitives") { }

    protected override Types.BuildingPad PreferredCast(object data) => data is DB.Architecture.BuildingPad pad ? new Types.BuildingPad(pad) : null;
  }

  public class CurtainGridLine : GraphicalElementT<Types.CurtainGridLine, DB.CurtainGridLine>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("A2DD571E-729C-4F69-BD34-2769583D329B");

    public CurtainGridLine() : base("Curtain Grid Line", "Curtain Grid Line", "Represents a Revit document curtain grid line element.", "Params", "Revit Primitives") { }

    protected override Types.CurtainGridLine PreferredCast(object data) => data is DB.CurtainGridLine gridLine ? new Types.CurtainGridLine(gridLine) : null;
  }

  public class CurtainSystem : GraphicalElementT<Types.CurtainSystem, DB.CurtainSystem>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("E94B20E9-C2AA-4FC7-939D-ECD071EA45DA");

    public CurtainSystem() : base("Curtain System", "Curtain System", "Represents a Revit document curtain system element.", "Params", "Revit Primitives") { }

    protected override Types.CurtainSystem PreferredCast(object data) => data is DB.CurtainSystem system ? new Types.CurtainSystem(system) : null;
  }

  public class Ceiling : GraphicalElementT<Types.Ceiling, DB.Ceiling>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("7FCEA93D-8CDE-446C-9167-E8B590342C66");

    public Ceiling() : base("Ceiling", "Ceiling", "Represents a Revit document ceiling element.", "Params", "Revit Primitives") { }

    protected override Types.Ceiling PreferredCast(object data) => data is DB.Ceiling ceiling ? new Types.Ceiling(ceiling) : null;
  }

  public class Floor : GraphicalElementT<Types.Floor, DB.Floor>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("45616DF6-59BF-4480-A133-8F9B3BA27AF1");

    public Floor() : base("Floor", "Floor", "Represents a Revit document floor element.", "Params", "Revit Primitives") { }

    protected override Types.Floor PreferredCast(object data) => data is DB.Floor florr ? new Types.Floor(florr) : null;
  }

  public class Roof : GraphicalElementT<Types.Roof, DB.RoofBase>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("D75E33E2-2508-42E6-AEF1-B05759A495AB");

    public Roof() : base("Roof", "Roof", "Represents a Revit document roof element.", "Params", "Revit Primitives") { }

    protected override Types.Roof PreferredCast(object data) => data is DB.RoofBase roof ? new Types.Roof(roof) : null;
  }

  public class Wall : GraphicalElementT<Types.Wall, DB.Wall>
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("15AD6BF9-63AD-462B-985D-F6B8C2299465");

    public Wall() : base("Wall", "Wall", "Represents a Revit document wall element.", "Params", "Revit Primitives") { }

    protected override Types.Wall PreferredCast(object data) => data is DB.Wall wall ? new Types.Wall(wall) : null;
  }

}
