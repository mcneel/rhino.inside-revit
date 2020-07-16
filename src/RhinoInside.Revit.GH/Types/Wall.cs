using System;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Geometry.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class Wall : HostObject
  {
    public override string TypeDescription => "Represents a Revit wall element";
    protected override Type ScriptVariableType => typeof(DB.Wall);
    public static explicit operator DB.Wall(Wall value) =>
      value?.IsValid == true ? value.Document.GetElement(value) as DB.Wall : default;

    public Wall() { }
    public Wall(DB.Wall host) : base(host) { }

    public override Plane Location
    {
      get
      {
        var wall = (DB.Wall) this;

        if (wall?.Location is DB.LocationCurve curveLocation)
        {
          var start = curveLocation.Curve.Evaluate(0.0, normalized: true).ToPoint3d();
          var end = curveLocation.Curve.Evaluate(1.0, normalized: true).ToPoint3d();
          var axis = end - start;
          var origin = start + (axis * 0.5);
          var perp = axis.PerpVector();
          return new Plane(origin, axis, perp);
        }

        return base.Location;
      }
    }

    public override Curve Curve
    {
      get
      {
        var wall = (DB.Wall) this;

        return wall?.Location is DB.LocationCurve curveLocation ?
          curveLocation.Curve.ToCurve() :
          null;
      }
    }
  }
}
