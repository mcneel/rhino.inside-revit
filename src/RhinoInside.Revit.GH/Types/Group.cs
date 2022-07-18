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
      get
      {
        if (Value is ARDB.Group group && group.Category.Id.IntegerValue == (int) ARDB.BuiltInCategory.OST_IOSModelGroups)
        {
          try
          {
            var doc = group.Document;
            using (doc.RollBackScope())
            {
              var plane = Plane.WorldXY.ToPlane();
              var sketchPlane = ARDB.SketchPlane.Create(doc, plane);
              var circle = ARDB.Arc.Create(plane, 1.0, 0.0, 2.0 * Math.PI);
              var modelCurve = default(ARDB.ModelCurve);
              var identity = default(ARDB.Group);
              if (doc.IsFamilyDocument)
              {
                modelCurve = doc.FamilyCreate.NewModelCurve(circle, sketchPlane);
                identity = doc.FamilyCreate.NewGroup(new ARDB.ElementId[] { modelCurve.Id });
              }
              else
              {
                modelCurve = doc.Create.NewModelCurve(circle, sketchPlane);
                identity = doc.Create.NewGroup(new ARDB.ElementId[] { modelCurve.Id });
              }

              group.ChangeTypeId(identity.GetTypeId());
              var geometries = group.GetMemberIds();
              if
              (
                geometries.Count == 1 &&
                group.Document.GetElement(geometries[0]) is ARDB.ModelCurve transformedModelCurve &&
                transformedModelCurve.GeometryCurve is ARDB.Arc transformedCircle
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
          catch { }
        }

        return base.Location;
      }
    }
    #endregion

    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var bbox = ClippingBox;
      if (!bbox.IsValid)
        return;

      bbox.Inflate(0.5 * Revit.ModelUnits);

      foreach (var edge in bbox.GetEdges() ?? Enumerable.Empty<Line>())
        args.Pipeline.DrawPatternedLine(edge.From, edge.To, args.Color, 0x000000F0, args.Thickness);
    }
  }
}
