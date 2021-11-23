using System;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

  [Kernel.Attributes.Name("Curtain System")]
  public class CurtainSystem : HostObject
  {
    protected override Type ValueType => typeof(ARDB.CurtainSystem);
    public new ARDB.CurtainSystem Value => base.Value as ARDB.CurtainSystem;
    public static explicit operator ARDB.CurtainSystem(CurtainSystem value) => value?.Value;

    public CurtainSystem() { }
    public CurtainSystem(ARDB.CurtainSystem host) : base(host) { }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.CurtainSystem system && system.Location is ARDB.LocationCurve curveLocation)
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
      Value is ARDB.CurtainSystem system && system.Location is ARDB.LocationCurve curveLocation ?
      curveLocation.Curve.ToCurve() :
      default;
  }
}
