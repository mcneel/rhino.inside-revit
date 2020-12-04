using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.DirectShapes
{
  using Kernel.Attributes;

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
      using (var ga = GeometryEncoder.Context.Push(element))
      {
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
                        ga.MaterialId =
                        (
                          materialIndex < materialCount ?
                          materials[materialIndex++]?.Id :
                          materials[materialCount - 1]?.Id
                        ) ??
                        DB.ElementId.InvalidElementId;
                      }

                      var subShape = x.ToShape();
                      materialIds?.AddRange(Enumerable.Repeat(ga.MaterialId, subShape.Length));
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
      "Add Geometry DirectShape", "GeoDShape",
      "Given its Geometry, it adds a DirectShape element to the active Revit document",
      "Revit", "DirectShape"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GraphicalElement(), "DirectShape", "DS", "New DirectShape", GH_ParamAccess.item);
    }

    void ReconstructDirectShapeByGeometry
    (
      DB.Document doc,
      ref DB.Element element,

      [Optional] string name,
      Optional<DB.Category> category,
      IList<IGH_GeometricGoo> geometry,
      [Optional] IList<DB.Material> material
    )
    {
      SolveOptionalCategory(ref category, doc, DB.BuiltInCategory.OST_GenericModel, nameof(geometry));

      if (element is DB.DirectShape directShape && directShape.Category.Id == category.Value.Id) { }
      else directShape = DB.DirectShape.CreateElement(doc, category.Value.Id);

      directShape.Name = name ?? string.Empty;
      directShape.SetShape(BuildShape(directShape, geometry, material, out var paintIds));

      ReplaceElement(ref element, directShape);

      PaintElementSolids(directShape, paintIds);
    }
  }

  public class DirectShapeTypeByGeometry : ReconstructDirectShapeComponent
  {
    public override Guid ComponentGuid => new Guid("25DCFE8E-5BE9-460C-80E8-51B7041D8FED");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public DirectShapeTypeByGeometry() : base
    (
      "Add DirectShapeType", "DShapeTyp",
      "Given its Geometry, it reconstructs a DirectShapeType to the active Revit document",
      "Revit", "DirectShape"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "Type", "T", "New DirectShapeType", GH_ParamAccess.item);
    }

    void ReconstructDirectShapeTypeByGeometry
    (
      DB.Document doc,
      ref DB.ElementType elementType,

      string name,
      Optional<DB.Category> category,
      IList<IGH_GeometricGoo> geometry,
      [Optional] IList<DB.Material> material
    )
    {
      SolveOptionalCategory(ref category, doc, DB.BuiltInCategory.OST_GenericModel, nameof(geometry));

      if (elementType is DB.DirectShapeType directShapeType && directShapeType.Category.Id == category.Value.Id) { }
      else directShapeType = DB.DirectShapeType.Create(doc, name, category.Value.Id);

      directShapeType.Name = name;
      directShapeType.SetShape(BuildShape(directShapeType, geometry, material, out var paintIds));

      ReplaceElement(ref elementType, directShapeType);

      PaintElementSolids(directShapeType, paintIds);
    }
  }

  public class DirectShapeByLocation : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("A811EFA4-8DE2-46F3-9F88-3D4F13FE40BE");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public DirectShapeByLocation() : base
    (
      "Add DirectShape", "DShape",
      "Given its location, it reconstructs a DirectShape into the active Revit document",
      "Revit", "DirectShape"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GraphicalElement(), "DirectShape", "DS", "New DirectShape", GH_ParamAccess.item);
    }

    void ReconstructDirectShapeByLocation
    (
      DB.Document doc,
      ref DB.Element element,

      [Description("Location where to place the element. Point or plane is accepted.")]
      Rhino.Geometry.Plane location,
      DB.DirectShapeType type
    )
    {
      if (element is DB.DirectShape directShape && directShape.Category.Id == type.Category.Id) { }
      else directShape = DB.DirectShape.CreateElement(doc, type.Category.Id);

      if (directShape.TypeId != type.Id)
        directShape.SetTypeId(type.Id);

      using (var library = DB.DirectShapeLibrary.GetDirectShapeLibrary(doc))
      {
        if (!library.ContainsType(type.UniqueId))
          library.AddDefinitionType(type.UniqueId, type.Id);
      }

      using (var transform = Rhino.Geometry.Transform.PlaneToPlane(Rhino.Geometry.Plane.WorldXY, location).ToTransform())
      {
        directShape.SetShape(DB.DirectShape.CreateGeometryInstance(doc, type.UniqueId, transform));
      }

      var parametersMask = new DB.BuiltInParameter[]
      {
        DB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
        DB.BuiltInParameter.ELEM_FAMILY_PARAM,
        DB.BuiltInParameter.ELEM_TYPE_PARAM
      };

      ReplaceElement(ref element, directShape, parametersMask);
    }
  }
}
