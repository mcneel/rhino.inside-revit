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

  public class AddDirectShapeBrep : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("5ADE9AE3-588C-4285-ABC5-09DEB92C6660");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public AddDirectShapeBrep() : base
    (
      name: "Add DirectShape (Brep)",
      nickname: "B-Shape",
      description: "Given a Brep, it adds a Brep shape to the active Revit document",
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
        new Param_Brep
        {
          Name = "Brep",
          NickName = "B",
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

    const string _Shape_ = "Brep";

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
          if (!Params.GetData(DA, "Brep", out Brep brep)) return null;

          // Compute
          shape = Reconstruct
          (
            shape,
            doc.Value,
            brep
          );

          DA.SetData(_Shape_, shape);
          return shape;
        }
      );
    }

    static readonly ARDB.ElementId GenericModel = new ARDB.ElementId(ARDB.BuiltInCategory.OST_GenericModel);

    bool Reuse(ref ARDB.DirectShape shape)
    {
      if (shape is null) return false;
      if (shape.Category.Id != GenericModel) return false;
      return true;
    }

    ARDB.DirectShape Reconstruct(ARDB.DirectShape shape, ARDB.Document doc, Brep brep)
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
        var center = bbox?.GetCenter();

        shape.Pinned = false;
        shape.SetShape(Array.Empty<ARDB.GeometryObject>());
        if (center is object) shape.Location.Move(-center);
      }

      if (shape is object)
      {
        if (brep?.IsValid is true)
        {
          using (var ctx = GeometryEncoder.Context.Push(shape))
          {
            var bbox = brep.GetBoundingBox(accurate: false);
            var transform = Transform.Translation(bbox.Center / Revit.ModelUnits - Point3d.Origin);
            var inverse = Transform.Translation(Point3d.Origin - bbox.Center);

            ctx.RuntimeMessage = (severity, message, invalidGeometry) =>
            {
              invalidGeometry = invalidGeometry?.Duplicate();
              invalidGeometry?.Transform(transform);
              AddGeometryConversionError((GH_RuntimeMessageLevel) severity, message, invalidGeometry);
            };

            try
            {
              brep.Transform(inverse);
              shape.Pinned = false;
              shape.SetShape(brep.ToShape());
              shape.Location.Move(bbox.Center.ToXYZ());
            }
            catch (ConversionException e)
            {
              throw new Exceptions.RuntimeArgumentException("Brep", e.Message, brep);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException e)
            {
              if (e.GetType() == typeof(Autodesk.Revit.Exceptions.ArgumentException))
                throw new Exceptions.RuntimeArgumentException("Brep", "Input geometry does not satisfy DirectShape validation criteria.", brep);

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
