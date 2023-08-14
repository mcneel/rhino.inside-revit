using System;
using Rhino.Geometry;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Linked Element")]
  public class Instance : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.Instance);
    public new ARDB.Instance Value => base.Value as ARDB.Instance;

    public Instance() { }
    public Instance(ARDB.Instance instance) : base(instance) { }

    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      return element is ARDB.Instance && !(element is ARDB.FamilyInstance);
    }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.Instance instance)
        {
          var(origin, basisX, basisY) = instance.GetLocation();
          return new Plane(origin.ToPoint3d(), basisX.Direction.ToVector3d(), basisY.Direction.ToVector3d());
        }

        return base.Location;
      }
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is ARDB.Instance instance)
      {
        var bbox = Type.Value.get_BoundingBox(default).ToBox();
        return bbox.IsValid ?
          bbox.GetBoundingBox(xform * instance.GetTransform().ToTransform()) :
          instance.GetBoundingBoxXYZ().ToBox().GetBoundingBox(xform);
      }

      return NaN.BoundingBox;
    }

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB.Instance instance)
      {
        var bbox = Type.Value.get_BoundingBox(default).ToBoundingBox();
        if (bbox.IsValid)
        {
          args.Pipeline.PushModelTransform(instance.GetTransform().ToTransform());

          foreach (var edge in bbox.GetEdges())
              args.Pipeline.DrawPatternedLine(edge.From, edge.To, args.Color, 0x00003333, args.Thickness);

          args.Pipeline.PopModelTransform();
          return;
        }
      }

      base.DrawViewportWires(args);
    }
  }
}
