using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  [ComponentVersion(introduced: "1.14")]
  public class ViewRangeElevations : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("02234E1D-062E-49F0-BAEE-8917589C2533");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => string.Empty;

    public ViewRangeElevations() : base
    (
      name: "View Range",
      nickname: "V-Range",
      description: "Get-Set view range",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.View>("View", "V", "View to query"),
      ParamDefinition.Create<Parameters.LevelConstraint>("Primary Range : Top", "T", "Top elevation", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.LevelConstraint>("Primary Range : Cut plane", "C", "Cut plane elevation", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.LevelConstraint>("Primary Range : Bottom", "B", "Botton elevation", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.LevelConstraint>("View Depth : Level", "D", "View depth elevation", optional: true, relevance: ParamRelevance.Primary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.View>("View", "V", "View"),
      ParamDefinition.Create<Param_Interval>("Primary Range", "PR", "Primary range interval", relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Parameters.LevelConstraint>("Primary Range : Top", "T", "Top elevation", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.LevelConstraint>("Primary Range : Cut plane", "C", "Cut plane elevation", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.LevelConstraint>("Primary Range : Bottom", "B", "Botton elevation", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Interval>("View Depth", "VD", "View depth interval", relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Parameters.LevelConstraint>("View Depth : Level", "D", "View depth elevation", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Interval>("View Range", "VR", "View range interval", relevance: ParamRelevance.Secondary),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.ViewPlan view)) return;
      else Params.TrySetData(DA, "View", () => view);

      if (!Params.TryGetData(DA, "Primary Range : Top", out Types.LevelConstraint primaryTop)) return;
      if (!Params.TryGetData(DA, "Primary Range : Cut plane", out Types.LevelConstraint primaryCut)) return;
      if (!Params.TryGetData(DA, "Primary Range : Bottom", out Types.LevelConstraint primaryBottom)) return;
      if (!Params.TryGetData(DA, "View Depth : Level", out Types.LevelConstraint depthBottom)) return;

      if (view.Value is ARDB.ViewPlan viewPlan)
      {
        if (primaryTop is object || primaryCut is object || primaryBottom is object || depthBottom is object)
        {
          StartTransaction(view.Document);
          using (var viewRange = viewPlan.GetViewRange())
          {
            SetLevelOffset(view, viewRange, ARDB.PlanViewPlane.TopClipPlane, primaryTop);
            SetLevelOffset(view, viewRange, ARDB.PlanViewPlane.CutPlane, primaryCut);
            SetLevelOffset(view, viewRange, ARDB.PlanViewPlane.BottomClipPlane, primaryBottom);
            SetLevelOffset(view, viewRange, ARDB.PlanViewPlane.ViewDepthPlane, depthBottom);

            viewPlan.SetViewRange(viewRange);
            view.Document.Regenerate();
          }
        }

        using (var viewRange = viewPlan.GetViewRange())
        {
          Params.TrySetData
          (
            DA, "Primary Range", () =>
            new Interval
            (
              GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.BottomClipPlane).Elevation,
              GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.TopClipPlane).Elevation
            )
          );
          Params.TrySetData(DA, "Primary Range : Top",        () => GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.TopClipPlane));
          Params.TrySetData(DA, "Primary Range : Cut plane",  () => GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.CutPlane));
          Params.TrySetData(DA, "Primary Range : Bottom",     () => GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.BottomClipPlane));
          Params.TrySetData
          (
            DA, "View Depth", () =>
            new Interval
            (
              GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.ViewDepthPlane).Elevation,
              GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.BottomClipPlane).Elevation
            )
          );
          Params.TrySetData(DA, "View Depth : Level", () => GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.ViewDepthPlane));
          Params.TrySetData
          (
            DA, "View Range", () =>
            new Interval
            (
              GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.ViewDepthPlane).Elevation,
              GetLevelOffset(view, viewRange, ARDB.PlanViewPlane.TopClipPlane).Elevation
            )
          );
        }
      }
    }

    static Types.LevelConstraint GetLevelOffset(Types.ViewPlan view, ARDB.PlanViewRange viewRange, ARDB.PlanViewPlane plane)
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

    private static void SetLevelOffset(Types.ViewPlan view, ARDB.PlanViewRange viewRange, ARDB.PlanViewPlane plane, Types.LevelConstraint constraint)
    {
      if (constraint is null) return;

      if (constraint.IsLevelConstraint(out var level, out var offset))
      {
        viewRange.SetLevelId(plane, level.Id);
        viewRange.SetOffset(plane, offset / Revit.ModelUnits);
      }
      else if (constraint.IsOffset(out var o))
      {
        viewRange.SetLevelId(plane, view.GenLevelId);
        viewRange.SetOffset(plane, o / Revit.ModelUnits);
      }
      else if (constraint.IsElevation(out var elevation))
      {
        viewRange.SetLevelId(plane, view.GenLevelId);
        viewRange.SetOffset(plane, (elevation - view.GenLevel.Elevation) / Revit.ModelUnits);
      }
      else if (constraint.IsUnlimited())
      {
        viewRange.SetLevelId(plane, ARDB.PlanViewRange.Unlimited);
      }
    }
  }
}
