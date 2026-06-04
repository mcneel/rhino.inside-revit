using System;
using System.Linq;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;
  using Rhino.Geometry;
  using RhinoInside.Revit.Convert.Geometry;

  [Kernel.Attributes.Name("Railing")]
  public sealed class Railing : GeometricElement, ISketchAccess
  {
    protected override Type ValueType => typeof(ARDB.Architecture.Railing);
    public new ARDB.Architecture.Railing Value => base.Value as ARDB.Architecture.Railing;

    public Railing() { }
    public Railing(ARDB.Architecture.Railing railing) : base(railing) { }

    #region Location
    public override Plane Location
    {
      get
      {
        if (Value is ARDB.Architecture.Railing railing)
        {
          if (railing.GetPath().FirstOrDefault().TryGetLocation(out var rO, out var rX, out var rY))
            return new Plane(rO.ToPoint3d(), rX.Direction.ToVector3d(), rY.Direction.ToVector3d());
        }

        return NaN.Plane;
      }
    }

    public override Curve Curve => Value is ARDB.Architecture.Railing railing ?
      Curve.JoinCurves(railing.GetPath().Select(x => x.ToCurve()), GeometryTolerance.Model.VertexTolerance)[0] :
      default;

    public override void SetCurve(Curve curve, bool keepJoins = false)
    {
      if (Value is ARDB.Architecture.Railing railing && curve is object)
      {
        if (Curve.GeometryEquals(curve, GeometryTolerance.Model.VertexTolerance) == false)
        {
          railing.SetPath(curve.ToBoundedCurveLoop());
          InvalidateGraphics();
        }
      }
    }
    #endregion

    #region ISketchAccess
    public Sketch Sketch => GetElement<Sketch>(Value?.GetSketchId());
    public Plane SketchPlane => Sketch?.Location ?? NaN.Plane;
    #endregion

    #region IHostElementAccess
    public override GraphicalElement HostElement =>
      Value is ARDB.Architecture.Railing railing && railing.HasHost ?
      GetElement<GraphicalElement>(railing.HostId) :
      base.HostElement;
    #endregion
  }


  [Kernel.Attributes.Name("Rail")]
  public sealed class ContinuousRail : GeometricElement
  {
    protected override Type ValueType => typeof(ARDB.Architecture.ContinuousRail);
    public new ARDB.Architecture.ContinuousRail Value => base.Value as ARDB.Architecture.ContinuousRail;

    public ContinuousRail() { }
    public ContinuousRail(ARDB.Architecture.ContinuousRail rail) : base(rail) { }

    #region Location
    public override Plane Location => Railing?.Location ?? NaN.Plane;

    public override Curve Curve => Value is ARDB.Architecture.ContinuousRail railing ?
      Curve.JoinCurves(railing.GetPath().Select(x => x.ToCurve()), GeometryTolerance.Model.VertexTolerance)[0] :
      default;
    #endregion

    #region IHostElementAccess
    public Railing Railing =>
      Value is ARDB.Architecture.ContinuousRail rail && rail.HostRailingId.IsValid() ?
      GetElement<Railing>(rail.HostRailingId) :
      null;

    public override GraphicalElement HostElement => Railing;
    #endregion
  }
}
