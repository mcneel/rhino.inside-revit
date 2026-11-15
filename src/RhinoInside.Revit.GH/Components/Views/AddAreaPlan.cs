using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  [ComponentVersion(introduced: "1.36")]
  public class AddAreaPlan : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("3E2753FA-B1B8-4923-B5BB-52FEA446C496");

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public AddAreaPlan() : base
    (
      name: "Add Area Plan",
      nickname: "AreaPlan",
      description: "Given a level, it adds an area plan to the active Revit document",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = "Level",
          NickName = "L",
          Description = "Reference level for the new area plan view",
        }
      ),
      new ParamDefinition
       (
        new Param_String
        {
          Name = "Name",
          NickName = "N",
          Description = "View name",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.AreaScheme()
        {
          Name = "Area Scheme",
          NickName = "S",
          Description = "View area scheme",
        }
      ),
      new ParamDefinition
      (
        new Parameters.AreaPlan()
        {
          Name = "Template",
          NickName = "T",
          Description = $"Template view plan (only parameters are copied)",
          Optional = true
        }, ParamRelevance.Occasional
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.AreaPlan()
        {
          Name = _View_,
          NickName = "AP",
          Description = $"Output {_View_}",
        }
      )
    };

    protected const string _View_ = "Area Plan";

    public static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.VIEW_NAME,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Level", out Types.Level level, x => x.IsValid)) return;
      var doc = level.Document;

      ReconstructElement<ARDB.ViewPlan>
      (
        doc, _View_, viewPlan =>
        {
          // Input
          if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return null;
          if (!Params.TryGetData(DA, "Area Scheme", out Types.AreaScheme areaScheme)) return null;
          Params.TryGetData(DA, "Template", out ARDB.ViewPlan template);

          // Compute
          StartTransaction(doc);
          if (CanReconstruct(_View_, out var untracked, ref viewPlan, doc, name, ARDB.ViewType.AreaPlan.ToString()))
            viewPlan = Reconstruct(viewPlan, level.Value, areaScheme.Value, name, template);

          DA.SetData(_View_, viewPlan);
          return untracked ? null : viewPlan;
        }
      );
    }

    bool Reuse(ARDB.ViewPlan viewPlan, ARDB.Level level, ARDB.AreaScheme areaScheme)
    {
      if (viewPlan is null) return false;
      if (!viewPlan.GenLevel.IsEquivalent(level)) return false;
      if (areaScheme.Id != viewPlan.AreaScheme.Id) return false;

      return true;
    }

    ARDB.ViewPlan Create(ARDB.Level level, ARDB.AreaScheme areaScheme)
    {
      var view = ARDB.ViewPlan.CreateAreaPlan(level.Document, areaScheme.Id, level.Id);
      view.SetDefaultPhaseFilter();
      return view;
    }

    ARDB.ViewPlan Reconstruct(ARDB.ViewPlan viewPlan, ARDB.Level level, ARDB.AreaScheme areaScheme, string name, ARDB.ViewPlan template)
    {
      if (!Reuse(viewPlan, level, areaScheme))
        viewPlan = Create(level, areaScheme);

      viewPlan.CopyParametersFrom(template, ExcludeUniqueProperties);
      if (name is object) viewPlan?.get_Parameter(ARDB.BuiltInParameter.VIEW_NAME).Update(name);

      return viewPlan;
    }
  }
}
