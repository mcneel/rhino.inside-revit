using System;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Revision Cloud")]
  public class RevisionCloud : GeometricElement, ISketchAccess
  {
    protected override Type ValueType => typeof(ARDB.RevisionCloud);
    public new ARDB.RevisionCloud Value => base.Value as ARDB.RevisionCloud;

    public RevisionCloud() { }
    public RevisionCloud(ARDB.RevisionCloud element) : base(element) { }

    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo(out target))
        return true;

      if (typeof(Q).IsAssignableFrom(typeof(Revision)))
      {
        target = (Q) (object) Types.Element.FromElementId(Document, Value?.RevisionId);
        return true;
      }

      return false;
    }

    #region ISketchAccess
    public Sketch Sketch => GetElement<Sketch>(Value?.GetSketchId());
    #endregion

    #region Properties
    public override Plane Location => Sketch.ProfilesPlane;
    public override Brep TrimmedSurface => Sketch.TrimmedSurface;
    #endregion
  }
}
