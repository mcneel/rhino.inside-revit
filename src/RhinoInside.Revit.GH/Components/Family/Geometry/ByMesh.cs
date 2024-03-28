using System;
using Rhino.Geometry;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Families
{
  [ComponentVersion(introduced: "1.21")]
  public class FamilyGeometryByMesh : Component
  {
    public override Guid ComponentGuid => new Guid("8C8CEC75-9A9C-4796-A96B-FD0635FE0371");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override string IconTag => "B";

    public FamilyGeometryByMesh()
    : base("Component Family Mesh", "FamForm", string.Empty, "Revit", "Component")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddMeshParameter("Mesh", "M", string.Empty, GH_ParamAccess.item);
      manager[manager.AddParameter(new Parameters.Category(), "Subcategory", "Sc", string.Empty, GH_ParamAccess.item)].Optional = true;
      manager[manager.AddParameter(new Parameters.Material(), "Material", "M", string.Empty, GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddMeshParameter("Mesh", "M", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var mesh = default(Rhino.Geometry.Mesh);
      if (!DA.GetData("Mesh", ref mesh))
        return;

      mesh = mesh.DuplicateMesh();

      var subCategoryId = default(ARDB.ElementId);
      if (DA.GetData("Subcategory", ref subCategoryId))
        mesh.TrySetUserString(ARDB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY.ToString(), subCategoryId);

      var materialId = default(ARDB.ElementId);
      if (DA.GetData("Material", ref materialId))
        mesh.TrySetUserString(ARDB.BuiltInParameter.MATERIAL_ID_PARAM.ToString(), materialId);

      DA.SetData("Mesh", mesh);
    }
  }
}
