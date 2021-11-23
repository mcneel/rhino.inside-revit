using System;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.DirectShapes
{
  using Convert.Geometry;
  using Kernel.Attributes;

  public class DirectShapeByMesh : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("5542506A-A09E-4EC9-92B4-F2B52417511C");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public DirectShapeByMesh() : base
    (
      name: "Add Mesh DirectShape",
      nickname: "MshDShape",
      description: "Given a Mesh, it adds a Mesh shape to the active Revit document",
      category: "Revit",
      subCategory: "DirectShape"
    )
    { }

    void ReconstructDirectShapeByMesh
    (
      [Optional, NickName("DOC")]
      ARDB.Document document,

      [ParamType(typeof(Parameters.GraphicalElement)), Name("Mesh"), NickName("M"), Description("New Mesh Shape")]
      ref ARDB.DirectShape element,

      Rhino.Geometry.Mesh mesh
    )
    {
      if (!ThrowIfNotValid(nameof(mesh), mesh))
        return;

      var genericModel = new ARDB.ElementId(ARDB.BuiltInCategory.OST_GenericModel);
      if (element is object && element.Category.Id == genericModel) { }
      else ReplaceElement(ref element, ARDB.DirectShape.CreateElement(document, genericModel));

      using (var ctx = GeometryEncoder.Context.Push(element))
      {
        ctx.RuntimeMessage = (severity, message, invalidGeometry) =>
          AddGeometryConversionError((GH_RuntimeMessageLevel) severity, message, invalidGeometry);

        element.SetShape(mesh.ToShape().OfType<ARDB.GeometryObject>().ToList());
      }
    }
  }
}
