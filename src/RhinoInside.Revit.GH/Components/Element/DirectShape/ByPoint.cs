using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.DirectShapes
{
  public class DirectShapeByPoint : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("7A889B89-C423-4ED8-91D9-5CECE1EE803D");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public DirectShapeByPoint() : base
    (
      "Add Point DirectShape", "PtDShape",
      "Given a Point, it adds a Point shape to the active Revit document",
      "Revit", "DirectShape"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GraphicalElement(), "Point", "P", "New PointShape", GH_ParamAccess.item);
    }

    void ReconstructDirectShapeByPoint
    (
      DB.Document doc,
      ref DB.Element element,

      Rhino.Geometry.Point3d point
    )
    {
      if (!ThrowIfNotValid(nameof(point), point))
        return;

      if (element is DB.DirectShape ds) { }
      else ds = DB.DirectShape.CreateElement(doc, new DB.ElementId(DB.BuiltInCategory.OST_GenericModel));

      using (var ctx = GeometryEncoder.Context.Push(ds))
      {
        ctx.RuntimeMessage = (severity, message, invalidGeometry) =>
          AddGeometryConversionError((GH_RuntimeMessageLevel) severity, message, invalidGeometry);

        var shape = new DB.Point[] { DB.Point.Create(point.ToXYZ()) };
        ds.SetShape(shape);
      }

      ReplaceElement(ref element, ds);
    }
  }
}
