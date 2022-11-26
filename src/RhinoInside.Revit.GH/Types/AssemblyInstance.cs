using System;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Assembly")]
  public class AssemblyInstance : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.AssemblyInstance);
    public new ARDB.AssemblyInstance Value => base.Value as ARDB.AssemblyInstance;

    public AssemblyInstance() { }
    public AssemblyInstance(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public AssemblyInstance(ARDB.AssemblyInstance assembly) : base(assembly) { }

    #region Location
    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is ARDB.AssemblyInstance instance)
      {
        var bbox = BoundingBox.Empty;

        foreach (var element in instance.GetMemberIds().Select(x => GraphicalElement.FromElementId(instance.Document, x)).OfType<GraphicalElement>())
          bbox.Union(element.GetBoundingBox(xform));

        return bbox;
      }

      return NaN.BoundingBox;
    }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.AssemblyInstance instance)
        {
          using (var transform = instance.GetTransform())
            return new Plane(transform.Origin.ToPoint3d(), transform.BasisX.ToVector3d(), transform.BasisY.ToVector3d());
        }

        return NaN.Plane;
      }
    }
    #endregion

    protected override bool GetClippingBox(out BoundingBox clippingBox)
    {
      clippingBox = GetBoundingBox(Transform.Identity);
      clippingBox.Inflate(0.5 * Revit.ModelUnits);
      return true;
    }

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      foreach (var edge in ClippingBox.GetEdges() ?? Enumerable.Empty<Line>())
        args.Pipeline.DrawPatternedLine(edge.From, edge.To, args.Color, 0x000000F0, args.Thickness);
    }
  }
}
