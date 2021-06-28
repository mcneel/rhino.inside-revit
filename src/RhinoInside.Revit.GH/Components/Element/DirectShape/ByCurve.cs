using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.DirectShapes
{
  public class DirectShapeByCurve : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("77F4FBDD-8A05-44A3-AC54-E52A79CF3E5A");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public DirectShapeByCurve() : base
    (
      name: "Add Curve DirectShape",
      nickname: "CrvDShape",
      description: "Given a Curve, it adds a Curve shape to the active Revit document",
      category: "Revit",
      subCategory: "DirectShape"
    )
    { }

    void ReconstructDirectShapeByCurve
    (
      DB.Document doc,

      [ParamType(typeof(Parameters.GraphicalElement)), Name("Curve"), NickName("C"), Description("New Curve Shape")]
      ref DB.DirectShape element,

      Rhino.Geometry.Curve curve
    )
    {
      if (!ThrowIfNotValid(nameof(curve), curve))
        return;

      var genericModel = new DB.ElementId(DB.BuiltInCategory.OST_GenericModel);
      if (element is object && element.Category.Id == genericModel) { }
      else ReplaceElement(ref element, DB.DirectShape.CreateElement(doc, genericModel));

      using (var ctx = GeometryEncoder.Context.Push(element))
      {
        ctx.RuntimeMessage = (severity, message, invalidGeometry) =>
          AddGeometryConversionError((GH_RuntimeMessageLevel) severity, message, invalidGeometry);

        element.SetShape(curve.ToShape().OfType<DB.GeometryObject>().ToList());
      }
    }
  }
}
