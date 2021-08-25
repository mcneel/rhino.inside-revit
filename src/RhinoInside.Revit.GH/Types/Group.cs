using System;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Group")]
  public class Group : GraphicalElement
  {
    protected override Type ValueType => typeof(DB.Group);
    public new DB.Group Value => base.Value as DB.Group;
    public static explicit operator DB.Group(Group value) => value?.Value;

    public Group() { }
    public Group(DB.Group value) : base(value) { }

    public override Level Level
    {
      get
      {
        if(Value is DB.Group group)
          return Types.Level.FromElement(group.GetParameterValue<DB.Level>(DB.BuiltInParameter.GROUP_LEVEL)) as Level;

        return default;
      }
    }

    public override Plane Location
    {
      get
      {
        if (Value is DB.Group group)
        {
          var doc = group.Document;
          using (doc.RollBackScope())
          {
            var plane = Plane.WorldXY.ToPlane();
            var sketchPlane = DB.SketchPlane.Create(doc, plane);
            var circle = DB.Arc.Create(plane, 1.0, 0.0, 2.0 * Math.PI);
            var modelCurve = default(DB.ModelCurve);
            var identity = default(DB.Group);
            if (doc.IsFamilyDocument)
            {
              modelCurve = doc.FamilyCreate.NewModelCurve(circle, sketchPlane);
              identity = doc.FamilyCreate.NewGroup(new DB.ElementId[] { modelCurve.Id });
            }
            else
            {
              modelCurve = doc.Create.NewModelCurve(circle, sketchPlane);
              identity = doc.Create.NewGroup(new DB.ElementId[] { modelCurve.Id });
            }

            group.ChangeTypeId(identity.GetTypeId());
            var geometries = group.GetMemberIds();
            if
            (
              geometries.Count == 1 &&
              group.Document.GetElement(geometries[0]) is DB.ModelCurve transformedModelCurve &&
              transformedModelCurve.GeometryCurve is DB.Arc transformedCircle
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

        return base.Location;
      }
    }

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var bbox = ClippingBox;
      if (!bbox.IsValid)
        return;

      foreach (var edge in bbox.GetEdges() ?? Enumerable.Empty<Line>())
        args.Pipeline.DrawPatternedLine(edge.From, edge.To, args.Color, 0x00003333, args.Thickness);
    }
    #endregion
  }
}
