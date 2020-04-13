using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
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
      ref DB.Element element,

      Rhino.Geometry.Mesh mesh
    )
    {
      var scaleFactor = 1.0 / Revit.ModelUnits;

      ThrowIfNotValid(nameof(mesh), mesh);

      if (element is DB.DirectShape ds) { }
      else ds = DB.DirectShape.CreateElement(doc, new DB.ElementId(DB.BuiltInCategory.OST_GenericModel));

      var shape = mesh.
                  ToHostMultiple(scaleFactor).
                  SelectMany(x => x.ToDirectShapeGeometry());

      ds.SetShape(shape.ToList());

      ReplaceElement(ref element, ds);
    }
  }
}
