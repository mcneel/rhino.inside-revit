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
    public static explicit operator DB.Mullion(Mullion value) =>
      value?.IsValid == true ? value.Document.GetElement(value) as DB.Mullion : default;

    public Mullion() { }
    public Mullion(DB.Mullion value) : base(value) { }

    public override Rhino.Geometry.Curve Curve
    {
      get
      {
        var mullion = (DB.Mullion) this;
        return mullion?.LocationCurve?.ToCurve();
      }
    }
  }

  public class MullionPosition : ElementType
  {
    public override string TypeName => "Revit Mullion Position";
    public override string TypeDescription => "Represents a Revit Mullion Postion";
    protected override Type ScriptVariableType => typeof(DB.ElementType);

    public MullionPosition() { }
    public MullionPosition(DB.Document doc, DB.ElementId id) : base(doc, id) { }

    public override string DisplayName
    {
      get
      {
        var element = (DB.ElementType) this;
        if (element is object)
          return $"{element.Category?.Name} : {element.GetFamilyName()} : {element.Name}";

        switch ((DBX.BuiltInMullionPositionId) Id.IntegerValue)
        {
          case DBX.BuiltInMullionPositionId.PerpendicularToFace: return "Perpendicular To Face";
          case DBX.BuiltInMullionPositionId.ParallelToGround: return "Parallel To Ground";
        }

        return base.DisplayName;
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

    public override string DisplayName
    {
      get
      {
        var element = (DB.FamilySymbol) this;
        if (element is object)
          return $"{element.Category?.Name} : {element.GetFamilyName()} : {element.Name}";

        switch ((DBX.BuiltInMullionProfileId) Id.IntegerValue)
        {
          case DBX.BuiltInMullionProfileId.Rectangular: return "Rectangular";
          case DBX.BuiltInMullionProfileId.Circular: return "Circular";
        }

        return base.DisplayName;
      }
    }
  }
}
