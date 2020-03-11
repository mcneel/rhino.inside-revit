using System;
using System.Collections.Generic;
using DB = Autodesk.Revit.DB;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Elements.DirectShape
{
  public class DirectShapeByPoint : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("7A889B89-C423-4ED8-91D9-5CECE1EE803D");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public DirectShapeByPoint() : base
    (
      "AddDirectShape.ByPoint", "ByPoint",
      "Given a Point, it adds a Point shape to the active Revit document",
      "Revit", "Geometry"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GeometricElement(), "Point", "P", "New PointShape", GH_ParamAccess.item);
    }

    void ReconstructDirectShapeByPoint
    (
      DB.Document doc,
      ref Autodesk.Revit.DB.Element element,

      Rhino.Geometry.Point3d point
    )
    {
      var scaleFactor = 1.0 / Revit.ModelUnits;

      ThrowIfNotValid(nameof(point), point);

      if (element is DB.DirectShape ds) { }
      else ds = DB.DirectShape.CreateElement(doc, new DB.ElementId(DB.BuiltInCategory.OST_GenericModel));

      ds.SetShape(new List<DB.GeometryObject> { DB.Point.Create((point * scaleFactor).ToHost()) });

      ReplaceElement(ref element, ds);
    }
  }
}
