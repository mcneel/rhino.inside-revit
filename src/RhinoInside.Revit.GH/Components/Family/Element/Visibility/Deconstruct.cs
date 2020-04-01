using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components
{
  public class FamilyElementVisibilityDeconstruct : Component
  {
    public override Guid ComponentGuid => new Guid("8065268E-1417-4C1B-8495-122E67721F4D");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override string IconTag => "V";

    public FamilyElementVisibilityDeconstruct()
    : base("Deconstruct Visibility", "Deconstruct Visibility", string.Empty, "Revit", "Family Element")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddIntegerParameter("Visibility", "V", string.Empty, GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddBooleanParameter("ViewSpecific", "V", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("PlanRCPCut", "RCP", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("TopBottom", "Z", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("FrontBack", "Y", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("LeftRight", "X", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("OnlyWhenCut", "CUT", string.Empty, GH_ParamAccess.item);

      manager.AddBooleanParameter("Coarse", "C", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("Medium", "M", string.Empty, GH_ParamAccess.item);
      manager.AddBooleanParameter("Fine", "F", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      int value = 0;
      if (!DA.GetData("Visibility", ref value))
        return;

      var viewSpecific = (value & 1 << 1) != 0;

      var planRCPCut =  (value & 1 << 2) != 0;
      var topBottom = (value & 1 << 3) != 0;
      var frontBack = (value & 1 << 4) != 0;
      var leftRight = (value & 1 << 5) != 0;
      var onlyWhenCut = (value & 1 << 6) != 0;

      var coarse = (value & 1 << 13) != 0;
      var medium = (value & 1 << 14) != 0;
      var fine = (value & 1 << 15) != 0;

      DA.SetData("ViewSpecific", viewSpecific);

      DA.SetData("PlanRCPCut", planRCPCut);
      DA.SetData("TopBottom", topBottom);
      DA.SetData("FrontBack", frontBack);
      DA.SetData("LeftRight", leftRight);
      DA.SetData("OnlyWhenCut", onlyWhenCut);

      DA.SetData("Coarse", coarse);
      DA.SetData("Medium", medium);
      DA.SetData("Fine", fine);
    }
  }
}
