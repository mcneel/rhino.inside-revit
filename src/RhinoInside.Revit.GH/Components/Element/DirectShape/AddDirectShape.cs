using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;
using OS = System.Environment;
#if RHINO_8
using Grasshopper.Rhinoceros.Model;
#endif

namespace RhinoInside.Revit.GH.Components.DirectShapes
{
  using Convert.Geometry;
  using External.DB.Extensions;

  public abstract class AddDirectShapeComponent : ElementTrackerComponent
  {
    protected AddDirectShapeComponent(string name, string nickname, string description, string category, string subCategory) :
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

    private static bool ThrowIfNotValid(string paramName, GeometryBase value)
    {
      if (value is null) throw new Exceptions.RuntimeArgumentNullException(paramName);
      if (!value.IsValidWithLog(out var log))
      {
        throw new Exceptions.RuntimeArgumentException
        (
          paramName,
          $"Input geometry is not valid.{OS.NewLine}{log}",
          value
        );
      }

      return true;
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

  public class AddDirectShapeType : AddDirectShapeComponent
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

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(Parameters.Document.AddTarget(), ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Param_String
        {
          Name = "Family Name",
          NickName = "FN",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String
        {
          Name = "Name",
          NickName = "N",
        }
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
        new Parameters.DirectShapeType()
        {
          Name = _Type_,
          NickName = "T",
          Description = $"Output {_Type_}",
        }
      )
    };

    const string _Type_ = "Type";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.DirectShapeType>
      (
        doc.Value, _Type_, type =>
        {
          if (!Params.GetData(DA, "Family Name", out string familyName, x => ElementNaming.IsValidName(x))) return null;
          if (!Params.GetData(DA, "Name", out string name, x => ElementNaming.IsValidName(x))) return null;
          if (!Params.GetData(DA, "Category", out Types.Category category, x => x.IsValid)) return null;
          if (!Params.GetDataList(DA, "Category", out IList<IGH_GeometricGoo> geometry)) return null;
          if (!Params.GetDataList(DA, "Materials", out IList<Types.Material> materials)) return null;

          var categoryId = category?.Id ?? new ARDB.ElementId(ARDB.BuiltInCategory.OST_GenericModel);
          if (!IsValidCategoryId(categoryId, doc.Value))
            throw new Exceptions.RuntimeArgumentException("Category", $"Input category may not be used as a DirectShape category. {{{categoryId}}}");

          if (type is null || type.Category.Id != categoryId)
            type = type.ReplaceElement(ARDB.DirectShapeType.Create(doc.Value, name, categoryId), Array.Empty<ARDB.BuiltInParameter>());

#if REVIT_2022
          type.SetFamilyName(familyName ?? name);
#else
          if (familyName is object)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "'Family Name' on DirectShape Types is only supported on Revit 2022 or above.");
#endif
          type.Name = name;

          var shape = BuildShape
          (
            type,
            Point3d.Origin, geometry,
            materials?.Select(x => x.Value).ToArray() ?? Array.Empty<ARDB.Material>(),
            out var paintIds
          );
          type.SetShape(shape);

          return type;
        }
      );
    }
  }

  public class AddDirectShapeInstance : ElementTrackerComponent
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

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(Parameters.Document.AddTarget(), ParamRelevance.Occasional),
      new ParamDefinition
       (
        new Param_Plane
        {
          Name = "Location",
          NickName = "L",
          Description = "Location where to place the element. Point or plane is accepted.",
        }
      ),
      new ParamDefinition
       (
        new Parameters.DirectShapeType
        {
          Name = "Type",
          NickName = "T",
          Description = "DirectShape type"
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
          Name = _DirectShape_,
          NickName = "DS",
          Description = $"Output {_DirectShape_}",
        }
      )
    };

    const string _DirectShape_ = "DirectShape";

    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
        ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
        ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
        ARDB.BuiltInParameter.ELEM_TYPE_PARAM
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.DirectShape>
      (
        doc.Value, _DirectShape_, directShape =>
        {
          // Input
          if (!Params.GetData(DA, "Location", out Plane? location, x => x.IsValid)) return null;
          if (!Params.GetData(DA, "Type", out ARDB.DirectShapeType type)) return null;

          if (directShape is object && directShape.Category.Id == type.Category.Id)
          {
            if (directShape.TypeId != type.Id)
              directShape.SetTypeId(type.Id);
          }
          else directShape = directShape.ReplaceElement
          (
            ARDB.DirectShape.CreateElement(doc.Value, type.Category.Id),
            ExcludeUniqueProperties
          );

          using (var library = ARDB.DirectShapeLibrary.GetDirectShapeLibrary(doc.Value))
          {
            if (!library.ContainsType(type.UniqueId))
              library.AddDefinitionType(type.UniqueId, type.Id);
          }

          using (var transform = Transform.PlaneToPlane(Plane.WorldXY, location.Value).ToTransform())
          {
            directShape.SetShape(ARDB.DirectShape.CreateGeometryInstance(doc.Value, type.UniqueId, transform));
          }

          DA.SetData(_DirectShape_, directShape);
          return directShape;
        }
      );
    }
  }
}
