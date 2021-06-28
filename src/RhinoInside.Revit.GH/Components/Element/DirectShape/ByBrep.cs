using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.DirectShapes
{
  public class DirectShapeByBrep : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("5ADE9AE3-588C-4285-ABC5-09DEB92C6660");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public DirectShapeByBrep() : base
    (
      "Add Brep DirectShape", "BrpDShape",
      "Given a Brep, it adds a Brep shape to the active Revit document",
      "Revit", "DirectShape"
    )
    { }

    void ReconstructDirectShapeByBrep
    (
      DB.Document doc,

      [ParamType(typeof(Parameters.GraphicalElement)), Name("Brep"), NickName("B"), Description("New Brep Shape")]
      ref DB.DirectShape element,

      Rhino.Geometry.Brep brep
    )
    {
      if (!ThrowIfNotValid(nameof(brep), brep))
        return;

      var genericModel = new DB.ElementId(DB.BuiltInCategory.OST_GenericModel);
      if (element is object && element.Category.Id == genericModel) { }
      else ReplaceElement(ref element, DB.DirectShape.CreateElement(doc, genericModel));

      using (var ctx = GeometryEncoder.Context.Push(element))
      {
        ctx.RuntimeMessage = (severity, message, invalidGeometry) =>
          AddGeometryConversionError((GH_RuntimeMessageLevel) severity, message, invalidGeometry); 

        element.SetShape(brep.ToShape().OfType<DB.GeometryObject>().ToList());
      }
    }
  }
}
