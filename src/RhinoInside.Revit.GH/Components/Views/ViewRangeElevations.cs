using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  [ComponentVersion(introduced: "1.14")]
  public class ViewRangeElevations : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("02234E1D-062E-49F0-BAEE-8917589C2533");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => string.Empty;

    public ViewRangeElevations() : base
    (
      name: "View Range Elevations",
      nickname: "V-RangeElev",
      description: "Query view range elevations",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.View>("View", "V", "View to query")
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Param_Interval>("Primary Range", "PR", "Primary range interval", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.LevelConstraint>("Primary Range : Top", "T", "Top elevation", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.LevelConstraint>("Primary Range : Cut plane", "C", "Cut plane elevation", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.LevelConstraint>("Primary Range : Bottom", "B", "Botton elevation", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Interval>("View Depth", "VD", "View depth interval", relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Parameters.LevelConstraint>("View Depth : Level", "D", "View depth elevation", relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Param_Interval>("View Range", "VR", "View range interval", relevance: ParamRelevance.Primary),
      //ParamDefinition.Create<Parameters.LevelConstraint>("Underlay : Level", "U", "Underlay bottom elevation", relevance: ParamRelevance.Secondary),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view)) return;
      if (view.Value is ARDB.ViewPlan viewPlan)
      {
        using (var viewRange = viewPlan.GetViewRange())
        {
          Params.TrySetData
          (
            DA, "Primary Range", () =>
            new Interval
            (
              GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.BottomClipPlane).Value.Elevation * Revit.ModelUnits,
              GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.TopClipPlane).Value.Elevation * Revit.ModelUnits
            )
          );
          Params.TrySetData(DA, "Primary Range : Top", () => GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.TopClipPlane));
          Params.TrySetData(DA, "Primary Range : Cut plane", () => GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.CutPlane));
          Params.TrySetData(DA, "Primary Range : Bottom", () => GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.BottomClipPlane));
          Params.TrySetData
          (
            DA, "View Depth", () =>
            new Interval
            (
              GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.ViewDepthPlane).Value.Elevation * Revit.ModelUnits,
              GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.BottomClipPlane).Value.Elevation * Revit.ModelUnits
            )
          );
          Params.TrySetData(DA, "View Depth : Level", () => GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.ViewDepthPlane));
          Params.TrySetData(DA, "Underlay : Level", () => GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.UnderlayBottom));
          Params.TrySetData
          (
            DA, "View Range", () =>
            new Interval
            (
              GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.ViewDepthPlane).Value.Elevation * Revit.ModelUnits,
              GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.TopClipPlane).Value.Elevation * Revit.ModelUnits
            )
          );
        }
      }
    }

    internal static Types.LevelConstraint GetLevelOffset(Types.View view, ARDB.PlanViewRange viewRange, ARDB.PlanViewPlane plane)
    {
      var level = default(Types.Level);
      var levelId = viewRange.GetLevelId(plane);
      if (levelId == ARDB.PlanViewRange.Current) level = view.GenLevel;
      else if (levelId == ARDB.PlanViewRange.LevelBelow) level = view.GetElement<Types.Level>(view.Document.GetNearestBaseLevel(view.Value.GenLevel.ProjectElevation, out var _));
      else if (levelId == ARDB.PlanViewRange.LevelAbove) level = view.GetElement<Types.Level>(view.Document.GetNearestTopLevel(view.Value.GenLevel.ProjectElevation, out var _));
      else if (levelId == ARDB.PlanViewRange.Unlimited)
      {
        switch (plane)
        {
          case ARDB.PlanViewPlane.CutPlane:         return new Types.LevelConstraint(view.GenLevel, double.PositiveInfinity);
          case ARDB.PlanViewPlane.TopClipPlane:     return new Types.LevelConstraint(view.GenLevel, double.PositiveInfinity);
          case ARDB.PlanViewPlane.BottomClipPlane:  return new Types.LevelConstraint(view.GenLevel, double.NegativeInfinity);
          case ARDB.PlanViewPlane.ViewDepthPlane:   return new Types.LevelConstraint(view.GenLevel, double.NegativeInfinity);
          case ARDB.PlanViewPlane.UnderlayBottom:   return new Types.LevelConstraint(view.GenLevel, double.NegativeInfinity);
        }
      }
      else level = view.GetElement<Types.Level>(levelId);

      return new Types.LevelConstraint(level, viewRange.GetOffset(plane) * Revit.ModelUnits);
    }
  }
}
