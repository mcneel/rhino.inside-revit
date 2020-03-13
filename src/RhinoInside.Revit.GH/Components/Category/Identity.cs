using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class CategoryIdentity : Component
  {
    public override Guid ComponentGuid => new Guid("D794361E-DE8C-4D0A-BC77-52293F27D3AA");
    protected override string IconTag => "ID";

    public CategoryIdentity()
    : base("Category Identity", "Identity", "Query category identity information", "Revit", "Category")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "Category", "C", "Category to query", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddTextParameter("Name", "N", "Category name", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Category(), "Parent", "P", "Category parent category", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Param_Enum<Types.CategoryType>(), "Type", "T", "Category type", GH_ParamAccess.item);
      manager.AddBooleanParameter("AllowsSubcategories", "A", "Category allows subcategories to be added", GH_ParamAccess.item);
      manager.AddBooleanParameter("AllowsParameters", "A", "Category allows bound parameters", GH_ParamAccess.item);
      manager.AddBooleanParameter("HasMaterialQuantities", "M", "Category has material quantities", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Category category = null;
      if (!DA.GetData("Category", ref category))
        return;

      DA.SetData("Name", category?.Name);
      DA.SetData("Parent", category?.Parent);
      DA.SetData("Type", category?.CategoryType);
      DA.SetData("AllowsSubcategories", category?.CanAddSubcategory);
      DA.SetData("AllowsParameters", category?.AllowsBoundParameters);
      DA.SetData("HasMaterialQuantities", category?.HasMaterialQuantities);
    }
  }
}
