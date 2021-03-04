using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.DirectShapes
{
  public class DirectShapeByMesh : ReconstructElementComponent
  {
    public override Guid ComponentGuid => new Guid("5542506A-A09E-4EC9-92B4-F2B52417511C");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public DirectShapeByMesh() : base
    (
      "Add Mesh DirectShape", "MshDShape",
      "Given a Mesh, it adds a Mesh shape to the active Revit document",
      "Revit", "DirectShape"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GraphicalElement(), "Mesh", "M", "New MeshShape", GH_ParamAccess.item);
    }

    void ReconstructDirectShapeByMesh
    (
      DB.Document doc,
      ref DB.DirectShape element,

      Rhino.Geometry.Mesh mesh
    )
    {
      if (!ThrowIfNotValid(nameof(mesh), mesh))
        return;

      if (element is DB.DirectShape ds) { }
      else ds = DB.DirectShape.CreateElement(doc, new DB.ElementId(DB.BuiltInCategory.OST_GenericModel));

      using (var ctx = GeometryEncoder.Context.Push(ds))
      {
        ctx.RuntimeMessage = (severity, message, invalidGeometry) =>
          AddGeometryConversionError((GH_RuntimeMessageLevel) severity, message, invalidGeometry);

        ds.SetShape(mesh.ToShape().OfType<DB.GeometryObject>().ToList());
      }

      ReplaceElement(ref element, ds);
    }
  }
}
