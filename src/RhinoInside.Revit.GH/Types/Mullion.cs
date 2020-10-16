using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;
using DBX = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class Mullion : FamilyInstance
  {
    public override string TypeName => "Revit Mullion";

    public override string TypeDescription => "Represents a Revit Mullion Element";
    protected override Type ScriptVariableType => typeof(DB.Mullion);
    public static explicit operator DB.Mullion(Mullion value) => value?.Value;
    public new DB.Mullion Value => base.Value as DB.Mullion;

    public Mullion() { }
    public Mullion(DB.Mullion value) : base(value) { }

    public override Rhino.Geometry.Curve Curve => Value?.LocationCurve.ToCurve();
  }

  public class MullionPosition : ElementType
  {
    public override string TypeName => "Revit Mullion Position";
    public override string TypeDescription => "Represents a Revit Mullion Postion";
    protected override Type ScriptVariableType => typeof(DB.ElementType);

    public MullionPosition() { }
    public MullionPosition(DB.Document doc, DB.ElementId id) : base(doc, id) { }

    public override string Name
    {
      get
      {
        switch ((DBX.BuiltInMullionPositionId) Id.IntegerValue)
        {
          case DBX.BuiltInMullionPositionId.PerpendicularToFace: return "Perpendicular To Face";
          case DBX.BuiltInMullionPositionId.ParallelToGround:    return "Parallel To Ground";
        }

        return base.Name;
      }
    }
  }

  public class MullionProfile : ElementType
  {
    public override string TypeName => "Revit Mullion Profile";
    public override string TypeDescription => "Represents a Revit Mullion Profile";
    protected override Type ScriptVariableType => typeof(DB.ElementType);

    public MullionProfile() { }
    public MullionProfile(DB.Document doc, DB.ElementId id) : base(doc, id) { }

    public override string Name
    {
      get
      {
        switch ((DBX.BuiltInMullionProfileId) Id.IntegerValue)
        {
          case DBX.BuiltInMullionProfileId.Rectangular: return "Rectangular";
          case DBX.BuiltInMullionProfileId.Circular:    return "Circular";
        }

        return base.Name;
      }
    }
  }
}
