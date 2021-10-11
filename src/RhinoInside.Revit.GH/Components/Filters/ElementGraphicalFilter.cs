using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Filters
{
  public class ElementLevelFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("B534489B-1367-4ACA-8FD8-D4B365CEEE0D");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "L";

    public ElementLevelFilter()
    : base("Level Filter", "LevelFltr", "Filter used to match elements associated to the given level", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Level(), "Levels", "L", "Levels to match", GH_ParamAccess.list);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var levels = new List<DB.Level>();
      if (!DA.GetDataList("Levels", levels))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      var filters = levels.Where(x => x is object).
                    Select(x => new DB.ElementLevelFilter(x.Id, inverted)).
                    ToList<DB.ElementFilter>();

      DA.SetData("Filter", inverted ? CompoundElementFilter.Intersect(filters) : CompoundElementFilter.Union(filters));
    }
  }

  public class ElementDesignOptionFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("1B197E82-3A65-43D4-AE47-FD25E4E6F2E5");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "D";

    public ElementDesignOptionFilter()
    : base("Design Option Filter", "OptnFltr", "Filter used to match elements associated to the given Design Option", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      var DesignOption = new Parameters.Element();
      DesignOption.PersistentData.Append(new Types.DesignOption());

      manager.AddParameter(DesignOption, "Design Option", "DO", "Design Option to match", GH_ParamAccess.item);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var designOption = default(Types.DesignOption);
      if (!DA.GetData("Design Option", ref designOption))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      DA.SetData("Filter", new DB.ElementDesignOptionFilter(designOption.Id, inverted));
    }
  }

  public class ElementPhaseStatusFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("805C21EE-5481-4412-A06C-7965761737E8");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "P";

    public ElementPhaseStatusFilter()
    : base("Phase Status Filter", "PhaseFltr", "Filter used to match elements associated to the given Phase status", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Phase(), "Phase", "P", "Phase to match", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Param_Enum<Types.ElementOnPhaseStatus>(), "Status", "S", "Phase status to match", GH_ParamAccess.list);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var phase = default(Types.Phase);
      if (!DA.GetData("Phase", ref phase))
        return;

      var status = new List<DB.ElementOnPhaseStatus>();
      if (!DA.GetDataList("Status", status))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      DA.SetData("Filter", new DB.ElementPhaseStatusFilter(phase.Id, status, inverted));
    }
  }

  public class ElementOwnerViewFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("CFB42D90-F9D4-4601-9EEF-C624E92A424D");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "V";

    public ElementOwnerViewFilter()
    : base("Owner View Filter", "ViewFltr", "Filter used to match elements associated to the given View", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      var View = new Parameters.View();
      View.PersistentData.Append(new Types.View());

      manager.AddParameter(View, "View", "V", "View to query", GH_ParamAccess.item);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var view = default(Types.View);
      if (!DA.GetData("View", ref view))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      DA.SetData("Filter", new DB.ElementOwnerViewFilter(view.Id, inverted));
    }
  }

  public class ElementSelectableInViewFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("AC546F16-C917-4CD1-9F8A-FBDD6330EB80");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "S";

    public ElementSelectableInViewFilter()
    : base("Selectable In View Filter", "SelFltr", "Filter used to match seletable elements into the given View", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.View(), "View", "V", "View to query", GH_ParamAccess.item);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var view = default(Types.View);
      if (!DA.GetData("View", ref view))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      DA.SetData("Filter", new Autodesk.Revit.UI.Selection.SelectableInViewFilter(view.Document, view.Id, inverted));
    }
  }
}
