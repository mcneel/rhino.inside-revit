using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using DB = Autodesk.Revit.DB;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace RhinoInside.Revit.GH.Components.Elements.DirectShape
{
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
      manager.AddParameter(new Parameters.Documents.ElementTypes.ElementType(), "Type", "T", "New DirectShapeType", GH_ParamAccess.item);
    }

    void ReconstructDirectShapeTypeByGeometry
    (
      DB.Document doc,
      ref Autodesk.Revit.DB.ElementType elementType,

      string name,
      Optional<Autodesk.Revit.DB.Category> category,
      IList<IGH_GeometricGoo> geometry,
      [Optional] IList<Autodesk.Revit.DB.Material> material
    )
    {
      var scaleFactor = 1.0 / Revit.ModelUnits;

      SolveOptionalCategory(ref category, doc, DB.BuiltInCategory.OST_GenericModel, nameof(geometry));

      if (elementType is DB.DirectShapeType directShapeType && directShapeType.Category.Id == category.Value.Id) { }
      else directShapeType = DB.DirectShapeType.Create(doc, name, category.Value.Id);

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
                                        DB.ElementId.InvalidElementId;
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
}
