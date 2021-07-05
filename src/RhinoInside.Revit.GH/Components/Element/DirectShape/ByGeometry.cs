using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.DirectShapes
{
  using Kernel.Attributes;
  using Convert.Geometry;
  using External.DB.Extensions;

  public abstract class ReconstructDirectShapeComponent : ReconstructElementComponent
  {
    protected ReconstructDirectShapeComponent(string name, string nickname, string description, string category, string subCategory) :
    base(name, nickname, description, category, subCategory) { }

    protected static Rhino.Geometry.GeometryBase AsGeometryBase(IGH_GeometricGoo obj)
    {
      var scriptVariable = obj.ScriptVariable();
      switch (scriptVariable)
      {
        case Rhino.Geometry.Point3d point: return new Rhino.Geometry.Point(point);
        case Rhino.Geometry.Line line: return new Rhino.Geometry.LineCurve(line);
        case Rhino.Geometry.Rectangle3d rect: return rect.ToNurbsCurve();
        case Rhino.Geometry.Arc arc: return new Rhino.Geometry.ArcCurve(arc);
        case Rhino.Geometry.Circle circle: return new Rhino.Geometry.ArcCurve(circle);
        case Rhino.Geometry.Ellipse ellipse: return ellipse.ToNurbsCurve();
        case Rhino.Geometry.Box box: return box.ToBrep();
      }

      return scriptVariable as Rhino.Geometry.GeometryBase;
    }

    protected IList<DB.GeometryObject> BuildShape
    (
      DB.Element element,
      IList<IGH_GeometricGoo> geometry,
      IList<DB.Material> materials,
      out IList<DB.ElementId> paintIds
    )
    {
      bool hasSolids = false;
      using (var ctx = GeometryEncoder.Context.Push(element))
      {
        ctx.RuntimeMessage = (severity, message, invalidGeometry) =>
          AddGeometryConversionError((GH_RuntimeMessageLevel) severity, message, invalidGeometry);

        var materialIndex = 0;
        var materialCount = materials?.Count ?? 0;

        var materialIds = materials is null ? null : new List<DB.ElementId>(geometry.Count);
        var shape = geometry.
                    Select(x => AsGeometryBase(x)).
                    Where(x => ThrowIfNotValid(nameof(geometry), x)).
                    SelectMany(x =>
                    {
                      if (materialCount > 0)
                      {
                        ctx.MaterialId =
                        (
                          materialIndex < materialCount ?
                          materials[materialIndex++]?.Id :
                          materials[materialCount - 1]?.Id
                        ) ??
                        DB.ElementId.InvalidElementId;
                      }

                      var subShape = x.ToShape();
                      materialIds?.AddRange(Enumerable.Repeat(ctx.MaterialId, subShape.Length));
                      if (!hasSolids) hasSolids = subShape.Any(s => s is DB.Solid);

                      return subShape;
                    }).
                    ToArray();

        paintIds = hasSolids ? materialIds : default;
        return shape;
      }
    }

    protected void PaintElementSolids(DB.Element element, IList<DB.ElementId> paintIds)
    {
      // If there are solids we may need to paint them
      if (paintIds is object)
      {
        var doc = element.Document;

        // Regenerate is necessary here, else 'element' may still have no geometry.
        doc.Regenerate();

        using (var elementGeometry = element.get_Geometry(new DB.Options() { DetailLevel = DB.ViewDetailLevel.Undefined }))
        {
          int index = 0;
          foreach (var geo in elementGeometry)
          {
            if (geo is DB.Solid solid)
            {
              var materialId = index < paintIds.Count ? paintIds[index] : DB.ElementId.InvalidElementId;
              foreach (var face in solid.Faces.Cast<DB.Face>())
              {
                if (materialId.IsValid())
                  doc.Paint(element.Id, face, materialId);
                else
                  doc.RemovePaint(element.Id, face);
              }
            }

            index++;
          }
        }
      }
    }
  }

  public class DirectShapeByGeometry : ReconstructDirectShapeComponent
  {
    public override Guid ComponentGuid => new Guid("0BFBDA45-49CC-4AC6-8D6D-ECD2CFED062A");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public DirectShapeByGeometry() : base
    (
      name: "Add Geometry DirectShape",
      nickname: "GeoDShape",
      description: "Given its Geometry, it adds a DirectShape element to the active Revit document",
      category: "Revit",
      subCategory: "DirectShape"
    )
    { }

    void ReconstructDirectShapeByGeometry
    (
      [Optional, NickName("DOC")]
      DB.Document document,

      [ParamType(typeof(Parameters.GraphicalElement)), NickName("DS"), Description("New DirectShape")]
      ref DB.DirectShape directShape,

      [Optional] string name,
      Optional<DB.Category> category,
      IList<IGH_GeometricGoo> geometry,
      [Optional] IList<DB.Material> material
    )
    {
      SolveOptionalCategory(ref category, document, DB.BuiltInCategory.OST_GenericModel, nameof(geometry));

      if (directShape is object && directShape.Category.Id == category.Value.Id) { }
      else ReplaceElement(ref directShape, DB.DirectShape.CreateElement(document, category.Value.Id));

      directShape.Name = name ?? string.Empty;
      directShape.SetShape(BuildShape(directShape, geometry, material, out var paintIds));

      PaintElementSolids(directShape, paintIds);
    }
  }

  public class DirectShapeTypeByGeometry : ReconstructDirectShapeComponent
  {
    public override Guid ComponentGuid => new Guid("25DCFE8E-5BE9-460C-80E8-51B7041D8FED");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public DirectShapeTypeByGeometry() : base
    (
      name: "Add DirectShapeType",
      nickname: "DShapeTyp",
      description: "Given its Geometry, it reconstructs a DirectShapeType to the active Revit document",
      category: "Revit",
      subCategory: "DirectShape"
    )
    { }

    void ReconstructDirectShapeTypeByGeometry
    (
      [Optional, NickName("DOC")]
      DB.Document document,

      [Description("New DirectShape Type")]
      ref DB.ElementType type,

      string name,
      Optional<DB.Category> category,
      IList<IGH_GeometricGoo> geometry,
      [Optional] IList<DB.Material> material
    )
    {
      SolveOptionalCategory(ref category, document, DB.BuiltInCategory.OST_GenericModel, nameof(geometry));

      if (type is DB.DirectShapeType directShapeType && directShapeType.Category.Id == category.Value.Id) { }
      else directShapeType = DB.DirectShapeType.Create(document, name, category.Value.Id);

      directShapeType.Name = name;
      directShapeType.SetShape(BuildShape(directShapeType, geometry, material, out var paintIds));

      ReplaceElement(ref type, directShapeType);

      PaintElementSolids(directShapeType, paintIds);
    }
  }

  public class DirectShapeByLocation : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("A811EFA4-8DE2-46F3-9F88-3D4F13FE40BE");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public DirectShapeByLocation() : base
    (
      name: "Add DirectShape",
      nickname: "DShape",
      description: "Given its location, it reconstructs a DirectShape into the active Revit document",
      category: "Revit",
      subCategory: "DirectShape"
    )
    { }

    void ReconstructDirectShapeByLocation
    (
      [Optional, NickName("DOC")]
      DB.Document document,

      [ParamType(typeof(Parameters.GraphicalElement)), NickName("DS"), Description("New DirectShape")]
      ref DB.DirectShape directShape,

      [Description("Location where to place the element. Point or plane is accepted.")]
      Rhino.Geometry.Plane location,
      DB.DirectShapeType type
    )
    {
      var parametersMask = new DB.BuiltInParameter[]
      {
        DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
        DB.BuiltInParameter.ELEM_FAMILY_PARAM,
        DB.BuiltInParameter.ELEM_TYPE_PARAM
      };

      if (directShape is object && directShape.Category.Id == type.Category.Id) { }
      else ReplaceElement(ref directShape, DB.DirectShape.CreateElement(document, type.Category.Id), parametersMask);

      if (directShape.TypeId != type.Id)
        directShape.SetTypeId(type.Id);

      using (var library = DB.DirectShapeLibrary.GetDirectShapeLibrary(document))
      {
        if (!library.ContainsType(type.UniqueId))
          library.AddDefinitionType(type.UniqueId, type.Id);
      }

      using (var transform = Rhino.Geometry.Transform.PlaneToPlane(Rhino.Geometry.Plane.WorldXY, location).ToTransform())
      {
        directShape.SetShape(DB.DirectShape.CreateGeometryInstance(document, type.UniqueId, transform));
      }
    }
  }
}
