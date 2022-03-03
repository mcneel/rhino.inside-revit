using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

  [Kernel.Attributes.Name("Curtain System")]
  public class CurtainSystem : HostObject, ICurtainGridsAccess
  {
    protected override Type ValueType => typeof(ARDB.CurtainSystem);
    public new ARDB.CurtainSystem Value => base.Value as ARDB.CurtainSystem;

    public CurtainSystem() { }
    public CurtainSystem(ARDB.CurtainSystem system) : base(system) { }

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

    public override Curve Curve => Value?.Location is ARDB.LocationCurve curveLocation ?
      curveLocation.Curve.ToCurve() : default;

    #region IGH_CurtainGridsAccess
    public IList<CurtainGrid> CurtainGrids => Value is ARDB.CurtainSystem system ?
      system.CurtainGrids?.Cast<ARDB.CurtainGrid>().Select(x => new CurtainGrid(system, x)).ToArray() :
      default;
    #endregion
  }
}
