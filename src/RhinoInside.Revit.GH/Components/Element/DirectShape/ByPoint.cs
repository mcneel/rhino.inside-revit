using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.DirectShapes
{
  public class DirectShapeByPoint : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("7A889B89-C423-4ED8-91D9-5CECE1EE803D");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public DirectShapeByPoint() : base
    (
      name: "Add Point DirectShape",
      nickname: "PtDShape",
      description: "Given a Point, it adds a Point shape to the active Revit document",
      category: "Revit",
      subCategory: "DirectShape"
    )
    { }

    void ReconstructDirectShapeByPoint
    (
      DB.Document doc,

      [ParamType(typeof(Parameters.GraphicalElement)), Name("Point"), NickName("P"), Description("New Point Shape")]
      ref DB.DirectShape element,

      Rhino.Geometry.Point3d point
    )
    {
      if (!ThrowIfNotValid(nameof(point), point))
        return;

      var genericModel = new DB.ElementId(DB.BuiltInCategory.OST_GenericModel);
      if (element is object && element.Category.Id == genericModel) { }
      else ReplaceElement(ref element, DB.DirectShape.CreateElement(doc, genericModel));

      using (var ctx = GeometryEncoder.Context.Push(element))
      {
        ctx.RuntimeMessage = (severity, message, invalidGeometry) =>
          AddGeometryConversionError((GH_RuntimeMessageLevel) severity, message, invalidGeometry);

        var shape = new DB.Point[] { DB.Point.Create(point.ToXYZ()) };
        element.SetShape(shape);
      }
    }
  }
}
