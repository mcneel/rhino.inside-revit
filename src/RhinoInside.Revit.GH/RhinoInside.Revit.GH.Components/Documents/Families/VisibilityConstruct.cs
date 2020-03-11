using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace RhinoInside.Revit.GH.Components.Documents.Families
{
  public class VisibilityConstruct : Component
  {
    public override Guid ComponentGuid => new Guid("10EA29D4-16AF-4060-89CE-F467F0069675");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override string IconTag => "V";

    public VisibilityConstruct()
    : base("Visibility.Construct", "Visibility.Construct", string.Empty, "Revit", "Family")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddBooleanParameter("ViewSpecific", "V", string.Empty, GH_ParamAccess.item, false);
      manager.AddBooleanParameter("PlanRCPCut", "RCP", string.Empty, GH_ParamAccess.item, true);
      manager.AddBooleanParameter("TopBottom", "Z", string.Empty, GH_ParamAccess.item, true);
      manager.AddBooleanParameter("FrontBack", "Y", string.Empty, GH_ParamAccess.item, true);
      manager.AddBooleanParameter("LeftRight", "X", string.Empty, GH_ParamAccess.item, true);
      manager.AddBooleanParameter("OnlyWhenCut", "CUT", string.Empty, GH_ParamAccess.item, false);

      manager.AddBooleanParameter("Coarse", "C", string.Empty, GH_ParamAccess.item, true);
      manager.AddBooleanParameter("Medium", "M", string.Empty, GH_ParamAccess.item, true);
      manager.AddBooleanParameter("Fine", "F", string.Empty, GH_ParamAccess.item, true);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddIntegerParameter("Visibility", "V", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var viewSpecific = false; if (!DA.GetData("ViewSpecific", ref viewSpecific)) return;

      var planRCPCut = false;   if (!DA.GetData("PlanRCPCut", ref planRCPCut)) return;
      var topBottom = false;    if (!DA.GetData("TopBottom", ref topBottom)) return;
      var frontBack = false;    if (!DA.GetData("FrontBack", ref frontBack)) return;
      var leftRight = false;    if (!DA.GetData("LeftRight", ref leftRight)) return;
      var onlyWhenCut = false;  if (!DA.GetData("OnlyWhenCut", ref onlyWhenCut)) return;

      var coarse = false;       if (!DA.GetData("Coarse", ref coarse)) return;
      var medium = false;       if (!DA.GetData("Medium", ref medium)) return;
      var fine = false;         if (!DA.GetData("Fine", ref fine)) return;

      int value = 0;
      if (viewSpecific) value |= 1 << 1;

      if (planRCPCut)   value |= 1 << 2;
      if (topBottom)    value |= 1 << 3;
      if (frontBack)    value |= 1 << 4;
      if (leftRight)    value |= 1 << 5;
      if (onlyWhenCut)  value |= 1 << 6;

      if (coarse)       value |= 1 << 13;
      if (medium)       value |= 1 << 14;
      if (fine)         value |= 1 << 15;

      DA.SetData("Visibility", value);
    }
  }
}
