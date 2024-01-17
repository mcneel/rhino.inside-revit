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
      manager.AddIntegerParameter("Visibility", "Vs", "Visibility/Graphics Overrides", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddBooleanParameter("Model", "M", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("PlanRCPCut", "RCP", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("TopBottom", "XY", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("FrontBack", "YZ", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("LeftRight", "XZ", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("OnlyWhenCut", "CUT", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("Coarse", "C", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("Medium", "M", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("Fine", "F", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var value = 0;
      if (!DA.GetData(0, ref value))
        return;

      var visibility = (ERDB.FamilyElementVisibility) value;

      DA.SetData(0, visibility.HasFlag(ERDB.FamilyElementVisibility.Model));
      DA.SetData("PlanRCPCut", visibility.HasFlag(ERDB.FamilyElementVisibility.PlanRCPCut));
      DA.SetData("TopBottom", visibility.HasFlag(ERDB.FamilyElementVisibility.TopBottom));
      DA.SetData("FrontBack", visibility.HasFlag(ERDB.FamilyElementVisibility.FrontBack));
      DA.SetData("LeftRight", visibility.HasFlag(ERDB.FamilyElementVisibility.LeftRight));
      DA.SetData("OnlyWhenCut", visibility.HasFlag(ERDB.FamilyElementVisibility.OnlyWhenCut));
      DA.SetData("Coarse", visibility.HasFlag(ERDB.FamilyElementVisibility.Coarse));
      DA.SetData("Medium", visibility.HasFlag(ERDB.FamilyElementVisibility.Medium));
      DA.SetData("Fine", visibility.HasFlag(ERDB.FamilyElementVisibility.Fine));
    }
  }
}
