using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  [ComponentVersion(introduced: "1.2", updated: "1.2.4")]
  public class ClusterViewsByType : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("F6B99FE2-19E1-4840-96C1-13873A0AECE8");

    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;

    public ClusterViewsByType() : base
    (
      name: "Cluster Views (Family)",
      nickname: "C-Views",
      description: "Split a list of views into separate clusters by their family",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.View>("Views", "V", "List of views to be split into branches based on type", GH_ParamAccess.list)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs = BuildOutputs();

    static ParamDefinition[] BuildOutputs()
    {
      var list = new List<ParamDefinition>();

      var values = Enum.GetValues(typeof(ARDB.ViewType)).
      Cast<ARDB.ViewType>().
      Select
      (
        x =>
        {
          var name = x.ToString();
          Types.ViewType.NamedValues.TryGetValue((int) x, out name);
          return (Name: name, ViewType: x);
        }
      ).Where(x => x.Name is object);

      foreach (var value in values.OrderBy(x => x.Name))
      {
        var param = default(IGH_Param);

        switch (value.ViewType)
        {
          case ARDB.ViewType.Undefined:
          case ARDB.ViewType.Internal:
          case ARDB.ViewType.ProjectBrowser:
          case ARDB.ViewType.SystemBrowser:
            continue;
          case ARDB.ViewType.ThreeD:                param = new Parameters.View3D() {NickName = "3D" }; break;
          case ARDB.ViewType.AreaPlan:              param = new Parameters.AreaPlan() { NickName = "AP" }; ; break;
          case ARDB.ViewType.CeilingPlan:           param = new Parameters.CeilingPlan() { NickName = "CP" }; break;
          case ARDB.ViewType.ColumnSchedule:        param = new Parameters.View() { NickName = "CS" }; break;
          case ARDB.ViewType.CostReport:            param = new Parameters.View() { NickName = "CR" }; break;
          case ARDB.ViewType.Detail:                param = new Parameters.DetailView() { NickName = "DE" }; break;
          case ARDB.ViewType.DraftingView:          param = new Parameters.ViewDrafting() { NickName = "DR" }; break;
          case ARDB.ViewType.Elevation:             param = new Parameters.ElevationView() { NickName = "EL" }; break;
          case ARDB.ViewType.FloorPlan:             param = new Parameters.FloorPlan() { NickName = "FP" }; ; break;
          case ARDB.ViewType.Legend:                param = new Parameters.View() { NickName = "LE" }; break;
          case ARDB.ViewType.LoadsReport:           param = new Parameters.View() { NickName = "LR" }; break;
          case ARDB.ViewType.PanelSchedule:         param = new Parameters.View() { NickName = "PS" }; break;
#if REVIT_2027
          case ARDB.ViewType.PressureLossReport:    param = new Parameters.View() { NickName = "PLR" }; break;
#else
          case ARDB.ViewType.PresureLossReport:     param = new Parameters.View() { NickName = "PLR" }; break;
#endif
          case ARDB.ViewType.Rendering:             param = new Parameters.View() { NickName = "R" }; break;
          case ARDB.ViewType.Report:                param = new Parameters.View() { NickName = "RT" }; break;
          case ARDB.ViewType.Section:               param = new Parameters.SectionView() { NickName = "SE" }; break;
          case ARDB.ViewType.DrawingSheet:          param = new Parameters.ViewSheet() { NickName = "SH" }; break;
          case ARDB.ViewType.EngineeringPlan:       param = new Parameters.StructuralPlan() { NickName = "SP" }; break;
#if REVIT_2021
          case ARDB.ViewType.SystemsAnalysisReport: param = new Parameters.View() { NickName = "SAR" }; break;
#endif
          case ARDB.ViewType.Walkthrough:           param = new Parameters.View3D() { NickName = "W" }; break;
          default:                                  param = new Parameters.View() { NickName = value.Name.Substring(0, 1) }; break;
        }

        param.Name = value.Name;
        param.Description = $"Views of type \"{value.Name}\"";
        param.Access = GH_ParamAccess.list;

        list.Add(new ParamDefinition(param, ParamRelevance.Primary));
      }

      return list.ToArray();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetDataList(DA, "Views", out IList<Types.View> views)) return;

      var viewTypes = Enum.GetValues(typeof(ARDB.ViewType));
      var clusters = new Dictionary<ARDB.ViewType, List<Types.View>>(viewTypes.Length);
      foreach (var view in views)
      {
        if (view?.Value is ARDB.View value)
        {
          if (!clusters.TryGetValue(value.ViewType, out var cluster))
            clusters.Add(value.ViewType, cluster = new List<Types.View>());

          cluster.Add(view);
        }
      }

      foreach(var cluster in clusters)
      {
        Types.ViewType.NamedValues.TryGetValue((int) cluster.Key, out var name);
        Params.TrySetDataList(DA, name, () => cluster.Value);
      }
    }
  }
}
