using System;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Geometry.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class CurtainSystem : HostObject
  {
    public override string TypeDescription => "Represents a Revit curtain system element";
    protected override Type ScriptVariableType => typeof(DB.CurtainSystem);
    public static explicit operator DB.CurtainSystem(CurtainSystem value) => value?.Value;
    public new DB.CurtainSystem Value => base.Value as DB.CurtainSystem;

    public CurtainSystem() { }
    public CurtainSystem(DB.CurtainSystem host) : base(host) { }

    public override Plane Location
    {
      get
      {
        if (Value is DB.CurtainSystem system && system.Location is DB.LocationCurve curveLocation)
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

    public override Curve Curve =>
      Value is DB.CurtainSystem system && system.Location is DB.LocationCurve curveLocation ?
      curveLocation.Curve.ToCurve() :
      default;
  }
}
