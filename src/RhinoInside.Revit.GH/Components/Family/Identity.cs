using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Families
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.0", updated: "1.15")]
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
      subCategory: "Type"
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
      manager.AddBooleanParameter("Shared", "S", "Identifies whether the family is a shared family.", GH_ParamAccess.item);
      manager.AddBooleanParameter("Parametric", "P", "Identifies whether the family contains parametric relations between some of its elements.", GH_ParamAccess.item);
      manager.AddBooleanParameter("User Created", "UC", "Identifies whether the family is defined by the user.", GH_ParamAccess.item);
      manager.AddBooleanParameter("In Place", "IP", "Identifies whether the family is an in-place family.", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Family", out Types.Family family)) return;

      DA.SetData("Category", family.Value.FamilyCategory);
      DA.SetData("Name", family.Nomen);
      Params.TrySetData(DA, "Shared", () => family.Value.get_Parameter(ARDB.BuiltInParameter.FAMILY_SHARED)?.AsBoolean());
      Params.TrySetData(DA, "Parametric", () => family.Value.IsParametric);
      Params.TrySetData(DA, "User Created", () => family.Value.IsUserCreated);
      Params.TrySetData(DA, "In Place", () => family.Value.IsInPlace);
    }
  }
}
