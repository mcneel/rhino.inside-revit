using System;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using Grasshopper.Kernel;

  [Kernel.Attributes.Name("MEP System")]
  public class MEPSystem : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.MEPSystem);
    public new ARDB.MEPSystem Value => base.Value as ARDB.MEPSystem;

    public MEPSystem() { }
    public MEPSystem(ARDB.MEPSystem value) : base(value) { }

    #region Location
    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is ARDB.MEPSystem system)
      {
        var bbox = system.BaseEquipment?.GetBoundingBoxXYZ().ToBox().GetBoundingBox(xform) ?? BoundingBox.Empty;

        if (!system.IsEmpty)
        {
          foreach (var element in system.Elements.Cast<ARDB.Element>().Select(GetElement<GraphicalElement>))
            bbox.Union(element.GetBoundingBox(xform));
        }

        return bbox;
      }

      return NaN.BoundingBox;
    }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.MEPSystem system && system.BaseEquipment is ARDB.Instance baseEquipment)
        {
          baseEquipment.GetLocation(out var origin, out var basisX, out var basisY);
          return new Plane(origin.ToPoint3d(), basisX.Direction.ToVector3d(), basisY.Direction.ToVector3d());
        }

        return base.Location;
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

  [Kernel.Attributes.Name("MEP System Type")]
  public class MEPSystemType : ElementType
  {
    protected override Type ValueType => typeof(ARDB.MEPSystemType);
    public new ARDB.MEPSystemType Value => base.Value as ARDB.MEPSystemType;

    public MEPSystemType() { }
    public MEPSystemType(ARDB.MEPSystemType value) : base(value) { }
  }
}
