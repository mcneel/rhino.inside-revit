using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components
{
  public class FamilyIdentity : Component
  {
    public override Guid ComponentGuid => new Guid("86875EF4-CE53-40F8-B446-97B4C8E2D54E");
    protected override string IconTag => "ID";

    public FamilyIdentity() : base
    (
      name: "Family Identity",
      nickname: "Identity",
      description: "Queries family identity information",
      category: "Revit",
      subCategory: "Family"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Family(), "Family", "F", "Family to query for its identity", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "Category", "C", "Category in which the Family resides", GH_ParamAccess.item);
      manager.AddTextParameter("Name", "N", "A human readable name for the Family", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      Autodesk.Revit.DB.Family family = null;
      if (!DA.GetData("Family", ref family))
        return;

      DA.SetData("Category", family?.FamilyCategory);
      DA.SetData("Name", family?.Name);
    }
  }
}
