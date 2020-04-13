using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class FamilyGeometryByBrep : Component
  {
    public override Guid ComponentGuid => new Guid("8A51D5A1-F463-492B-AE5B-B4F7870D106B");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override string IconTag => "B";

    public FamilyGeometryByBrep()
    : base("Family Form Geometry", "FamForm", string.Empty, "Revit", "Family")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddBrepParameter("Brep", "B", string.Empty, GH_ParamAccess.item);
      manager[manager.AddBooleanParameter("Visible", "V", string.Empty, GH_ParamAccess.item, true)].Optional = true;
      manager[manager.AddParameter(new Parameters.Category(), "Subcategory", "S", string.Empty, GH_ParamAccess.item)].Optional = true;
      manager[manager.AddIntegerParameter("Visibility", "S", string.Empty, GH_ParamAccess.item, -1)].Optional = true;
      manager[manager.AddParameter(new Parameters.Material(), "Material", "M", string.Empty, GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddBrepParameter("Brep", "B", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var brep = default(Rhino.Geometry.Brep);
      if (!DA.GetData("Brep", ref brep))
        return;

      brep = brep.DuplicateBrep();

      var visible = true;
      if (DA.GetData("Visible", ref visible))
        brep.SetUserString(DB.BuiltInParameter.IS_VISIBLE_PARAM.ToString(), visible ? null : "0");

      var subCategoryId = DB.ElementId.InvalidElementId;
      if (DA.GetData("Subcategory", ref subCategoryId))
        brep.SetUserString(DB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY.ToString(), subCategoryId.IsValid() ? subCategoryId.ToString() : null);

      var visibility = -1;
      if (DA.GetData("Visibility", ref visibility))
        brep.SetUserString(DB.BuiltInParameter.GEOM_VISIBILITY_PARAM.ToString(), visibility == -1 ? null : visibility.ToString());

      var materialId = DB.ElementId.InvalidElementId;
      if (DA.GetData("Material", ref materialId))
        brep.SetUserString(DB.BuiltInParameter.MATERIAL_ID_PARAM.ToString(), materialId.IsValid() ? materialId.ToString() : null);

      DA.SetData("Brep", brep);
    }
  }
}
