using System;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Curtain System")]
  public class CurtainSystem : HostObject
  {
    protected override Type ScriptVariableType => typeof(DB.CurtainSystem);
    public new DB.CurtainSystem Value => base.Value as DB.CurtainSystem;
    public static explicit operator DB.CurtainSystem(CurtainSystem value) => value?.Value;

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
