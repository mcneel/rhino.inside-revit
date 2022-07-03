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

    #region ISketchAccess
    public Sketch Sketch => Value is ARDB.RevisionCloud rvision ?
      new Sketch(rvision.GetSketch()) : default;
    #endregion

    #region Location
    public override Plane Location
    {
      get
      {
        if (Sketch is Sketch sketch)
        {
          var plane = sketch.ProfilesPlane;
          plane.Origin = plane.ClosestPoint(BoundingBox.Center);

          return plane;
        }

        return base.Location;
      }
    }
    #endregion
  }
}
