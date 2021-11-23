using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.DirectShapes
{
  using Convert.Geometry;
  using Kernel.Attributes;

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
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [ParamType(typeof(Parameters.GraphicalElement)), Name("Point"), NickName("P"), Description("New Point Shape")]
      ref ARDB.DirectShape element,

      Rhino.Geometry.Point3d point
    )
    {
      if (!ThrowIfNotValid(nameof(point), point))
        return;

      var genericModel = new ARDB.ElementId(ARDB.BuiltInCategory.OST_GenericModel);
      if (element is object && element.Category.Id == genericModel) { }
      else ReplaceElement(ref element, ARDB.DirectShape.CreateElement(document, genericModel));

      using (var ctx = GeometryEncoder.Context.Push(element))
      {
        ctx.RuntimeMessage = (severity, message, invalidGeometry) =>
          AddGeometryConversionError((GH_RuntimeMessageLevel) severity, message, invalidGeometry);

        var shape = new ARDB.Point[] { ARDB.Point.Create(point.ToXYZ()) };
        element.SetShape(shape);
      }
    }
  }
}
