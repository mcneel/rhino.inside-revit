using System;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Group")]
  public class Group : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.Group);
    public new ARDB.Group Value => base.Value as ARDB.Group;

    public Group() { }
    public Group(ARDB.Group value) : base(value) { }

    public override ARDB.ElementId LevelId => Value?.get_Parameter(ARDB.BuiltInParameter.GROUP_LEVEL).AsElementId();

    #region Location
    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is ARDB.Group group)
      {
        var bbox = BoundingBox.Empty;

        foreach (var element in group.GetMemberIds().Select(x => GraphicalElement.FromElementId(group.Document, x)).OfType<GraphicalElement>())
          bbox.Union(element.GetBoundingBox(xform));

        return bbox;
      }

      return NaN.BoundingBox;
    }

    public override Plane Location
    {
      get => Rhinoceros.InvokeInHostContext(() =>
      {
        if (Value is ARDB.Group group)
        {
          try
          {
            var doc = group.Document;
            using (doc.RollBackScope())
            {
              using (var create = doc.Create())
              {
                var curve = default(ARDB.CurveElement);
                if (OwnerView is View ownerView)
                {
                  var plane = ARDB.Plane.CreateByOriginAndBasis(ownerView.Value.Origin, ownerView.Value.RightDirection, ownerView.Value.UpDirection);
                  var circle = ARDB.Arc.Create(plane, 1.0, 0.0, 2.0 * Math.PI);
                  curve = create.NewDetailCurve(ownerView.Value, circle);
                }
                else
                {
                  var plane = ARDB.Plane.CreateByOriginAndBasis(XYZExtension.Zero, XYZExtension.BasisX, XYZExtension.BasisY);
                  var circle = ARDB.Arc.Create(plane, 1.0, 0.0, 2.0 * Math.PI);
                  curve = create.NewModelCurve(circle, ARDB.SketchPlane.Create(doc, plane));
                }
                var identity = create.NewGroup(new ARDB.ElementId[] { curve.Id });

                group.ChangeTypeId(identity.GetTypeId());
                var geometries = group.GetMemberIds();
                if
                (
                  geometries.Count == 1 &&
                  group.Document.GetElement(geometries[0]) is ARDB.CurveElement transformedCurve &&
                  transformedCurve.GeometryCurve is ARDB.Arc transformedCircle
                )
                {
                  return new Plane
                  (
                    transformedCircle.Center.ToPoint3d(),
                    transformedCircle.XDirection.ToVector3d(),
                    transformedCircle.YDirection.ToVector3d()
                  );
                }
              }
            }
          }
          catch { }
        }

        return base.Location;
      });
    }
    #endregion

    protected override bool GetClippingBox(out BoundingBox clippingBox)
    {
      clippingBox = GetBoundingBox(Transform.Identity);

      switch (Category?.Id.ToBuiltInCategory())
      {
        case ARDB.BuiltInCategory.OST_IOSModelGroups:  clippingBox.Inflate(0.5 * Revit.ModelUnits); break;
        case ARDB.BuiltInCategory.OST_IOSDetailGroups: clippingBox.Inflate(0.005 * Revit.ModelUnits, 0.005 * Revit.ModelUnits, 0.0); break;
      }

      return true;
    }

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      foreach (var edge in ClippingBox.GetEdges() ?? Enumerable.Empty<Line>())
        args.Pipeline.DrawPatternedLine(edge.From, edge.To, args.Color, 0x000000F0, args.Thickness);
    }
  }
}
