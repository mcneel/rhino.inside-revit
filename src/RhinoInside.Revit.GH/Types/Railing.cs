using System;
using System.Linq;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;
  using Rhino.Geometry;
  using RhinoInside.Revit.Convert.Geometry;

  [Kernel.Attributes.Name("Railing")]
  public class Railing : GeometricElement, ISketchAccess
  {
    protected override Type ValueType => typeof(ARDB.Architecture.Railing);
    public new ARDB.Architecture.Railing Value => base.Value as ARDB.Architecture.Railing;

    public Railing() { }
    public Railing(ARDB.Architecture.Railing railing) : base(railing) { }

    #region Location
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
    #endregion

    #region IHostElementAccess
    public override GraphicalElement HostElement =>
      Value is ARDB.Architecture.Railing railing && railing.HasHost ?
      GetElement<GraphicalElement>(railing.HostId) :
      base.HostElement;
    #endregion
  }
}
