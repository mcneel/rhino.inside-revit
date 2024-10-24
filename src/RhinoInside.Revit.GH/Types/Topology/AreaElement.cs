using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Area")]
  public class AreaElement : SpatialElement
  {
    protected override Type ValueType => typeof(ARDB.Area);
    public new ARDB.Area Value => base.Value as ARDB.Area;

    public AreaElement() { }
    public AreaElement(ARDB.Area element) : base(element) { }

    protected override double? ComputationOffset => Value is object ? 0.0 : default;

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (IsPlaced && IsEnclosed)
      {
        if (Boundaries is Curve[] boundaries)
        {
          var bbox = BoundingBox.Empty;
          foreach (var boundary in boundaries)
            bbox.Union(boundary.GetBoundingBox(xform));

          return bbox;
        }
      }

      return NaN.BoundingBox;
    }

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (IsPlaced && IsEnclosed)
      {
        if (Boundaries is Curve[] boundaries)
          foreach (var bopundary in boundaries)
            args.Pipeline.DrawCurve(bopundary, args.Color, args.Thickness);
      }
    }

    protected override void DrawViewportMeshes(GH_PreviewMeshArgs args) { }
  }
}
