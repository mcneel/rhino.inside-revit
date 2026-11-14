using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.DirectShapes
{
  using Convert.Geometry;
  using External.DB.Extensions;

  public class AddDirectShapeGeometry : AddDirectShapeComponent
  {
    public override Guid ComponentGuid => new Guid("0BFBDA45-49CC-4AC6-8D6D-ECD2CFED062A");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public AddDirectShapeGeometry() : base
    (
      name: "Add DirectShape (Geometry)",
      nickname: "G-Shape",
      description: "Given its Geometry, it adds a DirectShape element to the active Revit document",
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
        new Param_String
        {
          Name = "Name",
          NickName = "N",
          Optional = true,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Category
        {
          Name = "Category",
          NickName = "C",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Geometry
        {
          Name = "Geometry",
          NickName = "G",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.Material
        {
          Name = "Material",
          NickName = "M",
          Optional = true,
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
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
          NickName = "DS",
          Description = $"Output {_Shape_}",
        }
      )
    };

    const string _Shape_ = "DirectShape";

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
          if (!Params.TryGetData(DA, "Name", out string name, x => ElementNaming.IsValidName(x))) return null;
          if (!Params.TryGetData(DA, "Category", out Types.Category category, x => x.IsValid)) return null;
          if (!Params.GetDataList(DA, "Geometry", out IList<IGH_GeometricGoo> geometry)) return null;
          if (!Params.TryGetDataList(DA, "Material", out IList<Types.Material> materials)) return null;

          // Compute
          shape = Reconstruct
          (
            shape,
            doc.Value,
            name,
            category?.Id ?? GenericModel,
            geometry,
            materials?.Select(x => x.Value).ToArray() ?? Array.Empty<ARDB.Material>()
          );

          DA.SetData(_Shape_, shape);
          return shape;
        }
      );
    }

    static readonly ARDB.ElementId GenericModel = new ARDB.ElementId(ARDB.BuiltInCategory.OST_GenericModel);

    bool Reuse(ref ARDB.DirectShape shape, ARDB.ElementId categoryId)
    {
      if (shape is null) return false;
      if (shape.Category.Id != categoryId) return false;
      return true;
    }

    ARDB.DirectShape Reconstruct(ARDB.DirectShape shape, ARDB.Document doc, string name, ARDB.ElementId categoryId, IList<IGH_GeometricGoo> geometry, IList<ARDB.Material> materials)
    {
      if (!IsValidCategoryId(categoryId, doc))
        throw new Exceptions.RuntimeArgumentException("Category", $"Input category may not be used as a DirectShape category. {{{categoryId}}}");

      if (!Reuse(ref shape, categoryId))
      {
        shape = shape.ReplaceElement
        (
          ARDB.DirectShape.CreateElement(doc, categoryId),
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
        shape.Name = name;

        var bbox = BoundingBox.Empty;
        foreach (var g in geometry) bbox.Union(g.Boundingbox);

        if (bbox.IsValid)
        {
          using (var ctx = GeometryEncoder.Context.Push(shape))
          {
            var transform = Transform.Translation(bbox.Center / Revit.ModelUnits - Point3d.Origin);
            var inverse = Transform.Translation(Point3d.Origin - bbox.Center);

            ctx.RuntimeMessage = (severity, message, invalidGeometry) =>
            {
              invalidGeometry = invalidGeometry?.Duplicate();
              invalidGeometry?.Transform(transform);
              AddGeometryConversionError((GH_RuntimeMessageLevel) severity, message, invalidGeometry);
            };

            shape.Pinned = false;
            shape.SetShape(BuildShape(shape, bbox.Center, geometry, materials, out var paintIds));
            shape.Location.Move(bbox.Center.ToXYZ());
            PaintElementSolids(shape, paintIds);
          }
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"DirectShape geometry is empty. {{{shape.Id.ToString("D")}}}");
      }

      return shape;
    }
  }
}
