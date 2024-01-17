using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Families
{
  using ERDB = External.DB;

  public class FamilyElementVisibilityConstruct : Component
  {
    public override Guid ComponentGuid => new Guid("10EA29D4-16AF-4060-89CE-F467F0069675");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override string IconTag => "V";

    public FamilyElementVisibilityConstruct() : base
    (
      name: "Construct Visibility",
      nickname: "Visibility",
      description: "Construct Visibility/Graphics Overrides value",
      category: "Revit",
      subCategory: "Component"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddBooleanParameter("Model", "M", string.Empty, GH_ParamAccess.item, false);
      manager.AddBooleanParameter("PlanRCPCut", "RCP", string.Empty, GH_ParamAccess.item, true);
      manager.AddBooleanParameter("TopBottom", "XY", string.Empty, GH_ParamAccess.item, true);
      manager.AddBooleanParameter("FrontBack", "YZ", string.Empty, GH_ParamAccess.item, true);
      manager.AddBooleanParameter("LeftRight", "XZ", string.Empty, GH_ParamAccess.item, true);
      manager.AddBooleanParameter("OnlyWhenCut", "CUT", string.Empty, GH_ParamAccess.item, false);

      manager.AddBooleanParameter("Coarse", "C", string.Empty, GH_ParamAccess.item, true);
      manager.AddBooleanParameter("Medium", "M", string.Empty, GH_ParamAccess.item, true);
      manager.AddBooleanParameter("Fine", "F", string.Empty, GH_ParamAccess.item, true);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddIntegerParameter("Visibility", "Vs", "Visibility/Graphics Overrides", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var model = false; if (!DA.GetData(0, ref model)) return;
      var planRCPCut = false; if (!DA.GetData("PlanRCPCut", ref planRCPCut)) return;
      var topBottom = false; if (!DA.GetData("TopBottom", ref topBottom)) return;
      var frontBack = false; if (!DA.GetData("FrontBack", ref frontBack)) return;
      var leftRight = false; if (!DA.GetData("LeftRight", ref leftRight)) return;
      var onlyWhenCut = false; if (!DA.GetData("OnlyWhenCut", ref onlyWhenCut)) return;
      var coarse = false; if (!DA.GetData("Coarse", ref coarse)) return;
      var medium = false; if (!DA.GetData("Medium", ref medium)) return;
      var fine = false; if (!DA.GetData("Fine", ref fine)) return;

      var value = default(ERDB.FamilyElementVisibility);
      if (model) value |= ERDB.FamilyElementVisibility.Model;
      if (planRCPCut)   value |= ERDB.FamilyElementVisibility.PlanRCPCut;
      if (topBottom)    value |= ERDB.FamilyElementVisibility.TopBottom;
      if (frontBack)    value |= ERDB.FamilyElementVisibility.FrontBack;
      if (leftRight)    value |= ERDB.FamilyElementVisibility.LeftRight;
      if (onlyWhenCut)  value |= ERDB.FamilyElementVisibility.OnlyWhenCut;
      if (coarse) value |= ERDB.FamilyElementVisibility.Coarse;
      if (medium) value |= ERDB.FamilyElementVisibility.Medium;
      if (fine)   value |= ERDB.FamilyElementVisibility.FrontBack;

      DA.SetData(0, value);
    }
  }
}
