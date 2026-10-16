using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Families
{
  using ERDB = External.DB;

  public class FamilyElementVisibilityDeconstruct : Component
  {
    public override Guid ComponentGuid => new Guid("8065268E-1417-4C1B-8495-122E67721F4D");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override string IconTag => "V";

    public FamilyElementVisibilityDeconstruct() : base
    (
      name: "Deconstruct Visibility",
      nickname: "Deconstruct Visibility",
      description: "Deconstruct Visibility/Graphics Overrides value",
      category: "Revit",
      subCategory: "Component"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Param_Enum<Types.FamilyElementVisibility>(), "Visibility", "Vs", "Visibility/Graphics Overrides", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddBooleanParameter("Model", "M", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("Model : When Cut", "RCP", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("Model : Plan/RCP", "XY", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("Model : Front/Back", "YZ", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("Model : Left/Right", "XZ", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("Symbolic : When Cut", "CUT", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("Detail Level : Coarse", "C", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("Detail Level : Medium", "M", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("Detail Level : Fine", "F", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var value = 0;
      if (!DA.GetData(0, ref value))
        return;

      var visibility = (ERDB.FamilyElementVisibility) value;

      DA.SetData(0, visibility.HasFlag(ERDB.FamilyElementVisibility.Model));
      DA.SetData(1, visibility.HasFlag(ERDB.FamilyElementVisibility.PlanRCPCut));
      DA.SetData(2, visibility.HasFlag(ERDB.FamilyElementVisibility.TopBottom));
      DA.SetData(3, visibility.HasFlag(ERDB.FamilyElementVisibility.FrontBack));
      DA.SetData(4, visibility.HasFlag(ERDB.FamilyElementVisibility.LeftRight));
      DA.SetData(5, visibility.HasFlag(ERDB.FamilyElementVisibility.OnlyWhenCut));
      DA.SetData(6, visibility.HasFlag(ERDB.FamilyElementVisibility.Coarse));
      DA.SetData(7, visibility.HasFlag(ERDB.FamilyElementVisibility.Medium));
      DA.SetData(8, visibility.HasFlag(ERDB.FamilyElementVisibility.Fine));
    }
  }
}
