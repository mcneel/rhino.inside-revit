using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
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

      if (levels.Count == 0)
      {
        var nothing = new DB.ElementFilter[] { new DB.ElementIsElementTypeFilter(true), new DB.ElementIsElementTypeFilter(false) };
        DA.SetData("Filter", new DB.LogicalAndFilter(nothing));
      }
      else if (levels.Count == 1)
      {
        DA.SetData("Filter", new DB.ElementLevelFilter(levels[0]?.Id ?? DB.ElementId.InvalidElementId, inverted));
      }
      else
      {
        var filters = levels.Select(x => new DB.ElementLevelFilter(x?.Id ?? DB.ElementId.InvalidElementId, inverted)).ToList<DB.ElementFilter>();
        DA.SetData("Filter", new DB.LogicalOrFilter(filters));
      }
    }
  }

  public class ElementDesignOptionFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("1B197E82-3A65-43D4-AE47-FD25E4E6F2E5");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "D";

    public ElementDesignOptionFilter()
    : base("Design Option Filter", "DOptFiltr", "Filter used to match elements associated to the given Design Option", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager[manager.AddParameter(new Parameters.Element(), "Design Option", "DO", "Design Option to match", GH_ParamAccess.item)].Optional = true;
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var designOption = default(DB.DesignOption);
      var _DesignOption_ = Params.IndexOfInputParam("Design Option");
      if
      (
        Params.Input[_DesignOption_].DataType != GH_ParamData.@void &&
        !DA.GetData(_DesignOption_, ref designOption)
      )
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      DA.SetData("Filter", new DB.ElementDesignOptionFilter(designOption?.Id ?? DB.ElementId.InvalidElementId, inverted));
    }
  }

  public class ElementPhaseStatusFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("805C21EE-5481-4412-A06C-7965761737E8");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "P";

    public ElementPhaseStatusFilter()
    : base("Phase Status Filter", "PhStFiltr", "Filter used to match elements associated to the given Phase status", "Revit", "Filter")
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
    : base("Owner View Filter", "OViewFltr", "Filter used to match elements associated to the given View", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager[manager.AddParameter(new Parameters.View(), "View", "V", "View to match", GH_ParamAccess.item)].Optional = true;
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var view = default(DB.View);
      var _View_ = Params.IndexOfInputParam("View");
      if
      (
        Params.Input[_View_].DataType != GH_ParamData.@void &&
        !DA.GetData(_View_, ref view)
      )
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      DA.SetData("Filter", new DB.ElementOwnerViewFilter(view?.Id ?? DB.ElementId.InvalidElementId, inverted));
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
      manager.AddParameter(new Parameters.View(), "View", "V", "View to match", GH_ParamAccess.item);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var view = default(DB.View);
      if (!DA.GetData("View", ref view))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      DA.SetData("Filter", new Autodesk.Revit.UI.Selection.SelectableInViewFilter(view.Document, view.Id, inverted));
    }
  }
}
