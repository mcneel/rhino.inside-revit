using System;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.DirectShapes
{
  using Convert.Geometry;
  using Kernel.Attributes;

  public class DirectShapeByBrep : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("5ADE9AE3-588C-4285-ABC5-09DEB92C6660");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public DirectShapeByBrep() : base
    (
      name: "Add Brep DirectShape",
      nickname: "BrpDShape",
      description: "Given a Brep, it adds a Brep shape to the active Revit document",
      category: "Revit",
      subCategory: "DirectShape"
    )
    { }

    void ReconstructDirectShapeByBrep
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [ParamType(typeof(Parameters.GraphicalElement)), Name("Brep"), NickName("B"), Description("New Brep Shape")]
      ref ARDB.DirectShape element,

      Rhino.Geometry.Brep brep
    )
    {
      if (!ThrowIfNotValid(nameof(brep), brep))
        return;

      var genericModel = new ARDB.ElementId(ARDB.BuiltInCategory.OST_GenericModel);
      if (element is object && element.Category.Id == genericModel) { }
      else ReplaceElement(ref element, ARDB.DirectShape.CreateElement(document, genericModel));

      using (var ctx = GeometryEncoder.Context.Push(element))
      {
        ctx.RuntimeMessage = (severity, message, invalidGeometry) =>
          AddGeometryConversionError((GH_RuntimeMessageLevel) severity, message, invalidGeometry); 

        element.SetShape(brep.ToShape().OfType<ARDB.GeometryObject>().ToList());
      }
    }
  }
}
