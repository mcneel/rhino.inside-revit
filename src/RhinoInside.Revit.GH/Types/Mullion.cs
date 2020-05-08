using System;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class Mullion : GraphicalElement
  {
    public override string TypeDescription => "Represents a Revit Curtain Grid Mullion Element";
    protected override Type ScriptVariableType => typeof(DB.Mullion);
    public static explicit operator DB.Mullion(Mullion value) =>
      value.Document?.GetElement(value) as DB.Mullion;

    public Mullion() { }
    public Mullion(DB.Mullion value) : base(value) { }

    public override Rhino.Geometry.Curve Axis
    {
      get
      {
        var mullion = (DB.Mullion) this;
        var axisCurve = mullion?.LocationCurve?.ToCurve();

        // .LocationCurve might be null so let's return a zero-length curve for those
        // place the curve at mullion base point
        var basepoint = ((DB.LocationPoint) mullion.Location).Point.ToPoint3d();
        Rhino.Geometry.Curve zeroLengthCurve = new Rhino.Geometry.Line(basepoint, basepoint).ToNurbsCurve();

        return axisCurve ?? zeroLengthCurve;
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
          return $"{element.Category?.Name} : {element.FamilyName} : {element.Name}";

        switch (Id.IntegerValue)
        {
          case -3: return "Perpendicular To Face";
          case -2: return "Parallel To Ground";
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
          return $"{element.Category?.Name} : {element.FamilyName} : {element.Name}";

        switch (Id.IntegerValue)
        {
          case -3: return "Rectangular";
          case -2: return "Circular";
        }

        return base.DisplayName;
      }
    }
  }
}
