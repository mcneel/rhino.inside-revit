using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

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

    #region Location
    public override Plane Location
    {
      get
      {
        if (Value is ARDB.CurtainSystem system && system.Location is ARDB.LocationCurve curveLocation)
        {
          var start = curveLocation.Curve.GetEndPoint(ERDB.CurveEnd.Start).ToPoint3d();
          var end   = curveLocation.Curve.GetEndPoint(ERDB.CurveEnd.End).ToPoint3d();
          var direction  = end - start;
          var origin = start + (direction * 0.5);
          var perp = direction.PerpVector();
          return new Plane(origin, direction, perp);
        }

        return base.Location;
      }
    }
    #endregion

    #region IGH_CurtainGridsAccess
    public IList<CurtainGrid> CurtainGrids => Value is ARDB.CurtainSystem system ?
      system.CurtainGrids?.Cast<ARDB.CurtainGrid>().Select(x => new CurtainGrid(system, x)).ToArray() :
      default;
    #endregion
  }
}
