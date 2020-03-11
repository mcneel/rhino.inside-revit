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
      DB.Document doc,
      ref Autodesk.Revit.DB.Element element,

      [Optional] string name,
      Optional<Autodesk.Revit.DB.Category> category,
      IList<IGH_GeometricGoo> geometry,
      [Optional] IList<Autodesk.Revit.DB.Material> material
    )
    {
      var scaleFactor = 1.0 / Revit.ModelUnits;

      SolveOptionalCategory(ref category, doc, DB.BuiltInCategory.OST_GenericModel, nameof(geometry));

      if (element is DB.DirectShape ds && ds.Category.Id == category.Value.Id) { }
      else ds = DB.DirectShape.CreateElement(doc, category.Value.Id);

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
}
