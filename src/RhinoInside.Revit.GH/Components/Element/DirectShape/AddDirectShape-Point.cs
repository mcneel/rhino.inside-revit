using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.DirectShapes
{
  using Convert;
  using Convert.Geometry;
  using External.DB.Extensions;

  public class AddDirectShapePoint : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("7A889B89-C423-4ED8-91D9-5CECE1EE803D");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public AddDirectShapePoint() : base
    (
      name: "Add DirectShape (Point)",
      nickname: "P-Shape",
      description: "Given a Point, it adds a Point shape to the active Revit document",
      category: "Revit",
      subCategory: "DirectShape"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(Parameters.Document.AddTarget(), ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Param_Point
        {
          Name = "Point",
          NickName = "P",
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.DirectShape()
        {
          Name = _Shape_,
          NickName = _Shape_.Substring(0, 1),
          Description = $"Output {_Shape_}",
        }
      )
    };

    const string _Shape_ = "Point";

    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.DirectShape>
      (
        doc.Value, _Shape_, shape =>
        {
          // Input
          if (!Params.GetData(DA, "Point", out Point3d? point)) return null;

          // Compute
          shape = Reconstruct
          (
            shape,
            doc.Value,
            point
          );

          DA.SetData(_Shape_, shape);
          return shape;
        }
      );
    }

    static readonly ARDB.GeometryObject[] PointAtOrigin = new ARDB.Point[] { ARDB.Point.Create(XYZExtension.Zero) };
    static readonly ARDB.ElementId GenericModel = new ARDB.ElementId(ARDB.BuiltInCategory.OST_GenericModel);

    bool Reuse(ref ARDB.DirectShape shape)
    {
      if (shape is null) return false;
      if (shape.Category.Id != GenericModel) return false;
      return true;
    }

    ARDB.DirectShape Reconstruct(ARDB.DirectShape shape, ARDB.Document doc, Point3d? point)
    {
      if (!Reuse(ref shape))
      {
        shape = shape.ReplaceElement
        (
          ARDB.DirectShape.CreateElement(doc, GenericModel),
          ExcludeUniqueProperties
        );
      }
      else
      {
        var bbox = shape.GetBoundingBoxXYZ();
        var (min, max) = bbox;
        var center = bbox?.GetCenter();

        var location = point?.IsValid is true ? point.Value.ToXYZ() : default;
        if (min.AlmostEqualPoints(max) && location.AlmostEqualPoints(center))
          return shape;

        shape.Pinned = false;
        shape.SetShape(Array.Empty<ARDB.GeometryObject>());
        if (center is object) shape.Location.Move(-center);
      }

      if (shape is object)
      {
        if (point?.IsValid is true)
        {
          using (var ctx = GeometryEncoder.Context.Push(shape))
          {
            var transform = Transform.Translation(point.Value / Revit.ModelUnits - Point3d.Origin);
            var inverse = Transform.Translation(Point3d.Origin - point.Value);

            ctx.RuntimeMessage = (severity, message, invalidGeometry) =>
            {
              invalidGeometry = invalidGeometry?.Duplicate();
              invalidGeometry?.Transform(transform);
              AddGeometryConversionError((GH_RuntimeMessageLevel) severity, message, invalidGeometry);
            };

            try
            {
              shape.Pinned = false;
              shape.SetShape(PointAtOrigin);
              shape.Location.Move(point.Value.ToXYZ());
            }
            catch (ConversionException e)
            {
              throw new Exceptions.RuntimeArgumentException("Point", e.Message, point);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException e)
            {
              if (e.GetType() == typeof(Autodesk.Revit.Exceptions.ArgumentException))
                throw new Exceptions.RuntimeArgumentException("Point", "Input geometry does not satisfy DirectShape validation criteria.", point);

              throw;
            }
          }
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"DirectShape geometry is empty. {{{shape.Id.ToString("D")}}}");
      }

      return shape;
    }
  }
}
