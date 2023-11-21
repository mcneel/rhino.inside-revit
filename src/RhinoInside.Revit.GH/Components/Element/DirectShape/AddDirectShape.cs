using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;
#if RHINO_8
using Grasshopper.Rhinoceros.Model;
#endif

namespace RhinoInside.Revit.GH.Components.DirectShapes
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using Kernel.Attributes;

  public abstract class ReconstructDirectShapeComponent : ReconstructElementComponent
  {
    protected ReconstructDirectShapeComponent(string name, string nickname, string description, string category, string subCategory) :
    base(name, nickname, description, category, subCategory) { }

    protected static GeometryBase AsGeometryBase(IGH_GeometricGoo obj)
    {
#if RHINO_8
      if (obj is GH_InstanceReference iref)
      {
        if (iref.InstanceDefinition is ModelInstanceDefinition idef && GeometryEncoder.Context.Peek.Document is ARDB.Document document)
        {
          var definitionId = Guid.NewGuid();
          var library = ARDB.DirectShapeLibrary.GetDirectShapeLibrary(document);
          if (!library.Contains(definitionId.ToString()))
          {
            var shape = idef.Objects.SelectMany(x =>
            {
              if (x.CastTo(out IGH_GeometricGoo geo))
                return AsGeometryBase(geo).ToShape();

              return null;
            }).OfType<ARDB.GeometryObject>();

            library.AddDefinition(definitionId.ToString(), shape.ToList());
          }
          return new InstanceReferenceGeometry(definitionId, iref.Value.Xform);
        }
        else return null;
      }
#endif

      var scriptVariable = obj.ScriptVariable();
      switch (scriptVariable)
      {
        case Point3d point: return new Point(point);
        case Line line: return new LineCurve(line);
        case Rectangle3d rect: return new PolylineCurve(rect.ToPolyline());
        case Arc arc: return new ArcCurve(arc);
        case Circle circle: return new ArcCurve(circle);
        case Ellipse ellipse: return ellipse.ToNurbsCurve();
        case Box box: return box.ToBrep();
      }

      return scriptVariable as GeometryBase;
    }

    internal static readonly IList<ARDB.GeometryObject> ShapeEmpty = Array.Empty<ARDB.GeometryObject>();
    protected IList<ARDB.GeometryObject> BuildShape
    (
      ARDB.Element element,
      Point3d center,
      IList<IGH_GeometricGoo> geometry,
      IList<ARDB.Material> materials,
      out IList<ARDB.ElementId> paintIds
    )
    {
      bool hasSolids = false;
      using (var ctx = GeometryEncoder.Context.Push(element))
      {
        var transform = Transform.Identity;
        var inverse = Transform.Identity;

        ctx.RuntimeMessage = (severity, message, invalidGeometry) =>
        {
          invalidGeometry = invalidGeometry?.Duplicate();
          invalidGeometry?.Transform(transform);
          AddGeometryConversionError((GH_RuntimeMessageLevel) severity, message, invalidGeometry);
        };

        var materialIndex = 0;
        var materialCount = materials?.Count ?? 0;

        var materialIds = materials is null ? null : new List<ARDB.ElementId>(geometry.Count);
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
                        ARDB.ElementId.InvalidElementId;
                      }

                      transform = Transform.Translation(center / Revit.ModelUnits - Point3d.Origin);
                      inverse = Transform.Translation(Point3d.Origin - center);

                      x.Transform(inverse);
                      var subShape = x.ToShape();
                      materialIds?.AddRange(Enumerable.Repeat(ctx.MaterialId, subShape.Length));
                      if (!hasSolids) hasSolids = subShape.Any(s => s is ARDB.Solid);

                      return subShape;
                    }).
                    ToArray();

        paintIds = hasSolids ? materialIds : default;
        return shape;
      }
    }

    protected void PaintElementSolids(ARDB.Element element, IList<ARDB.ElementId> paintIds)
    {
      // If there are solids we may need to paint them
      if (paintIds is object)
      {
        var doc = element.Document;

        // Regenerate is necessary here, else 'element' may still have no geometry.
        doc.Regenerate();

        using (var elementGeometry = element.get_Geometry(new ARDB.Options() { DetailLevel = ARDB.ViewDetailLevel.Undefined }))
        {
          int index = 0;
          foreach (var geo in elementGeometry)
          {
            if (geo is ARDB.Solid solid)
            {
              var materialId = index < paintIds.Count ? paintIds[index] : ARDB.ElementId.InvalidElementId;
              foreach (var face in solid.Faces.Cast<ARDB.Face>())
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

    public static bool IsValidCategoryId(ARDB.ElementId categoryId, ARDB.Document doc)
    {
#if REVIT_2018
      // For some unknown reason Revit dislikes 'Coordination Model' category (Tested in Revit 2023).
      if (categoryId.ToBuiltInCategory() == ARDB.BuiltInCategory.OST_Coordination_Model)
        return false;
#endif
      return ARDB.DirectShape.IsValidCategoryId(categoryId, doc);
    }
  }

  public class AddDirectShapeGeometry : ReconstructDirectShapeComponent
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

    void ReconstructAddDirectShapeGeometry
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [ParamType(typeof(Parameters.GraphicalElement)), Name("DirectShape"), NickName("DS"), Description("New DirectShape")]
      ref ARDB.DirectShape element,

      [Optional] string name,
      Optional<ARDB.Category> category,
      IList<IGH_GeometricGoo> geometry,
      [Optional] IList<ARDB.Material> material
    )
    {
      SolveOptionalCategory(ref category, document, ARDB.BuiltInCategory.OST_GenericModel, nameof(geometry));

      if (IsValidCategoryId(category.Value.Id, document))
      {
        //Remove nulls.
        geometry = geometry.OfType<IGH_GeometricGoo>().ToArray();

        var bbox = BoundingBox.Empty;
        foreach (var g in geometry) bbox.Union(g.Boundingbox);

        if (element is object && element.Category.Id == category.Value.Id) element.Pinned = false;
        else ReplaceElement(ref element, ARDB.DirectShape.CreateElement(document, category.Value.Id));

        element.Name = name ?? string.Empty;
        element.SetShape(ReconstructDirectShapeComponent.ShapeEmpty);
        if (bbox.IsValid)
        {
          element.Pinned = false;
          element.Location.Move(-bbox.Center.ToXYZ());
          element.SetShape(BuildShape(element, bbox.Center, geometry, material, out var paintIds));
          element.Location.Move(bbox.Center.ToXYZ());
          PaintElementSolids(element, paintIds);
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"DirectShape geometry is empty. {{{element.Id.ToString("D")}}}");
      }
      else
      {
        element = null;
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"'{category.Value.Name}' category may not be used as a DirectShape category.");
      }
    }
  }

  public class AddDirectShapeType : ReconstructDirectShapeComponent
  {
    public override Guid ComponentGuid => new Guid("25DCFE8E-5BE9-460C-80E8-51B7041D8FED");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public AddDirectShapeType() : base
    (
      name: "Add DirectShape Type",
      nickname: "D-ShapeType",
      description: "Given its Geometry, it reconstructs a DirectShape Type to the active Revit document",
      category: "Revit",
      subCategory: "DirectShape"
    )
    { }

    void ReconstructAddDirectShapeType
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [Description("New DirectShape Type")]
      ref ARDB.ElementType type,

      [Optional, Name("Family Name"), NickName("FN")]
      string familyName,
      string name,
      Optional<ARDB.Category> category,
      IList<IGH_GeometricGoo> geometry,
      [Optional] IList<ARDB.Material> material
    )
    {
      SolveOptionalCategory(ref category, document, ARDB.BuiltInCategory.OST_GenericModel, nameof(geometry));

      if (IsValidCategoryId(category.Value.Id, document))
      {
        if (type is ARDB.DirectShapeType directShapeType && directShapeType.Category.Id == category.Value.Id) { }
        else directShapeType = ARDB.DirectShapeType.Create(document, name, category.Value.Id);

#if REVIT_2022
        directShapeType.SetFamilyName(familyName ?? name);
#else
        if (familyName is object)
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "'Family Name' on DirectShape Types is only supported on Revit 2022 or above.");
#endif
        directShapeType.Name = name;
        directShapeType.SetShape(BuildShape(directShapeType, Point3d.Origin, geometry, material, out var paintIds));

        ReplaceElement(ref type, directShapeType);

        PaintElementSolids(directShapeType, paintIds);
      }
      else
      {
        type = null;
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"'{category.Value.Name}' category may not be used as a DirectShape category.");
      }
    }
  }

  public class AddDirectShapeInstance : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("A811EFA4-8DE2-46F3-9F88-3D4F13FE40BE");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public AddDirectShapeInstance() : base
    (
      name: "Add DirectShape Instance",
      nickname: "D-Shape",
      description: "Given its location, it reconstructs a DirectShape into the active Revit document",
      category: "Revit",
      subCategory: "DirectShape"
    )
    { }

    void ReconstructAddDirectShapeInstance
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [ParamType(typeof(Parameters.GraphicalElement)), NickName("DS"), Description("New DirectShape")]
      ref ARDB.DirectShape directShape,

      [Description("Location where to place the element. Point or plane is accepted.")]
      Plane location,
      ARDB.DirectShapeType type
    )
    {
      var parametersMask = new ARDB.BuiltInParameter[]
      {
        ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
        ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
        ARDB.BuiltInParameter.ELEM_TYPE_PARAM
      };

      if (directShape is object && directShape.Category.Id == type.Category.Id) { }
      else ReplaceElement(ref directShape, ARDB.DirectShape.CreateElement(document, type.Category.Id), parametersMask);

      if (directShape.TypeId != type.Id)
        directShape.SetTypeId(type.Id);

      using (var library = ARDB.DirectShapeLibrary.GetDirectShapeLibrary(document))
      {
        if (!library.ContainsType(type.UniqueId))
          library.AddDefinitionType(type.UniqueId, type.Id);
      }

      using (var transform = Transform.PlaneToPlane(Plane.WorldXY, location).ToTransform())
      {
        directShape.SetShape(ARDB.DirectShape.CreateGeometryInstance(document, type.UniqueId, transform));
      }
    }
  }
}
