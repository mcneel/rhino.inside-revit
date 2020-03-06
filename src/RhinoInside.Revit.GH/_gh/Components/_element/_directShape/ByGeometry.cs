using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Autodesk.Revit.DB;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace RhinoInside.Revit.GH.Components
{
  public class DirectShapeByGeometry : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("0bfbda45-49cc-4ac6-8d6d-ecd2cfed062a");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public DirectShapeByGeometry() : base
    (
      "AddDirectShape.ByGeometry", "ByGeometry",
      "Given its Geometry, it adds a DirectShape element to the active Revit document",
      "Revit", "Geometry"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GeometricElement(), "DirectShape", "DS", "New DirectShape", GH_ParamAccess.item);
    }

    void ReconstructDirectShapeByGeometry
    (
      Document doc,
      ref Autodesk.Revit.DB.Element element,

      [Optional] string name,
      Optional<Autodesk.Revit.DB.Category> category,
      IList<IGH_GeometricGoo> geometry,
      [Optional] IList<Autodesk.Revit.DB.Material> material
    )
    {
      var scaleFactor = 1.0 / Revit.ModelUnits;

      SolveOptionalCategory(ref category, doc, BuiltInCategory.OST_GenericModel, nameof(geometry));

      if (element is DirectShape ds && ds.Category.Id == category.Value.Id) { }
      else ds = DirectShape.CreateElement(doc, category.Value.Id);

      using (var ga = Convert.Context.Push())
      {
        var materialIndex = 0;
        var materialCount = material?.Count ?? 0;

        var shape = geometry.
                    Select(x => AsGeometryBase(x)).
                    Select(x => { ThrowIfNotValid(nameof(geometry), x); return x; }).
                    SelectMany(x =>
                    {
                      if (materialCount > 0)
                      {
                        ga.MaterialId = (
                                         materialIndex < materialCount ?
                                         material[materialIndex++]?.Id :
                                         material[materialCount - 1]?.Id
                                        ) ??
                                        ElementId.InvalidElementId;
                      }

                      return x.ToHostMultiple(scaleFactor);
                    }).
                    SelectMany(x => x.ToDirectShapeGeometry()).
                    ToArray();

        ds.SetShape(shape);
      }

      ds.Name = name ?? string.Empty;

      ReplaceElement(ref element, ds);
    }

    Rhino.Geometry.GeometryBase AsGeometryBase(IGH_GeometricGoo obj)
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
  }

  public class DirectShapeTypeByGeometry : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("25DCFE8E-5BE9-460C-80E8-51B7041D8FED");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public DirectShapeTypeByGeometry() : base
    (
      "AddDirectShapeType.ByGeometry", "ByGeometry",
      "Given its Geometry, it reconstructs a DirectShapeType to the active Revit document",
      "Revit", "Type"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "Type", "T", "New DirectShapeType", GH_ParamAccess.item);
    }

    void ReconstructDirectShapeTypeByGeometry
    (
      Document doc,
      ref Autodesk.Revit.DB.ElementType elementType,

      string name,
      Optional<Autodesk.Revit.DB.Category> category,
      IList<IGH_GeometricGoo> geometry,
      [Optional] IList<Autodesk.Revit.DB.Material> material
    )
    {
      var scaleFactor = 1.0 / Revit.ModelUnits;

      SolveOptionalCategory(ref category, doc, BuiltInCategory.OST_GenericModel, nameof(geometry));

      if (elementType is DirectShapeType directShapeType && directShapeType.Category.Id == category.Value.Id) { }
      else directShapeType = DirectShapeType.Create(doc, name, category.Value.Id);

      using (var ga = Convert.Context.Push())
      {
        var materialIndex = 0;
        var materialCount = material?.Count ?? 0;

        var shape = geometry.
                    Select(x => AsGeometryBase(x)).
                    Select(x => { ThrowIfNotValid(nameof(geometry), x); return x; }).
                    SelectMany(x =>
                    {
                      if (materialCount > 0)
                      {
                        ga.MaterialId = (
                                         materialIndex < materialCount ?
                                         material[materialIndex++]?.Id :
                                         material[materialCount - 1]?.Id
                                        ) ??
                                        ElementId.InvalidElementId;
                      }

                      return x.ToHostMultiple(scaleFactor);
                    }).
                    SelectMany(x => x.ToDirectShapeGeometry()).
                    ToArray();

        directShapeType.SetShape(shape);
      }

      directShapeType.Name = name;

      ReplaceElement(ref elementType, directShapeType);
    }

    Rhino.Geometry.GeometryBase AsGeometryBase(IGH_GeometricGoo obj)
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
  }

  public class DirectShapeByLocation : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("A811EFA4-8DE2-46F3-9F88-3D4F13FE40BE");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public DirectShapeByLocation() : base
    (
      "AddDirectShape.ByLocation", "ByLocation",
      "Given its location, it reconstructs a DirectShape into the active Revit document",
      "Revit", "Build"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GeometricElement(), "DirectShape", "DS", "New DirectShape", GH_ParamAccess.item);
    }

    void ReconstructDirectShapeByLocation
    (
      Document doc,
      ref Autodesk.Revit.DB.Element element,

      [Description("Location where to place the element. Point or plane is accepted.")]
      Rhino.Geometry.Plane location,
      Autodesk.Revit.DB.DirectShapeType type
    )
    {
      var scaleFactor = 1.0 / Revit.ModelUnits;

      if (element is DirectShape ds && ds.Category.Id == type.Category.Id) { }
      else ds = DirectShape.CreateElement(doc, type.Category.Id);

      if(ds.TypeId != type.Id)
        ds.SetTypeId(type.Id);

      var library = DirectShapeLibrary.GetDirectShapeLibrary(doc);
      if (!library.ContainsType(type.UniqueId))
        library.AddDefinitionType(type.UniqueId, type.Id);

      var transform = Rhino.Geometry.Transform.PlaneToPlane(Rhino.Geometry.Plane.WorldXY, location.ChangeUnits(scaleFactor)).ToHost();
      ds.SetShape(DirectShape.CreateGeometryInstance(doc, type.UniqueId, transform));

      var parametersMask = new BuiltInParameter[]
      {
        BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
        BuiltInParameter.ELEM_FAMILY_PARAM,
        BuiltInParameter.ELEM_TYPE_PARAM
      };

      ReplaceElement(ref element, ds, parametersMask);
    }
  }
}
