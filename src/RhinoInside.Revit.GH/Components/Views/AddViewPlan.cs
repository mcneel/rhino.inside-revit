using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  public abstract class AddViewPlan : ElementTrackerComponent
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    protected AddViewPlan(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory)
    { }

    protected virtual ARDB.ViewType ViewType { get; }
    protected virtual ARDB.ElementTypeGroup ElementTypeGroup { get; }
    protected const string _View_ = "View";

    public static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
      ARDB.BuiltInParameter.VIEW_NAME,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Level", out Types.Level level, x => x.IsValid)) return;
      var doc = Types.Document.FromValue(level.Document);

      ReconstructElement<ARDB.ViewPlan>
      (
        doc.Value, _View_, viewPlan =>
        {
          // Input
          if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return null;
          if (!Parameters.ViewFamilyType.GetDataOrDefault(this, DA, "Type", out Types.ViewFamilyType type, doc, ElementTypeGroup)) return null;
          Params.TryGetData(DA, "Template", out ARDB.ViewPlan template);

          // Compute
          StartTransaction(doc.Value);
          if (CanReconstruct(_View_, out var untracked, ref viewPlan, doc.Value, name, ViewType.ToString()))
            viewPlan = Reconstruct(viewPlan, level.Value, type.Value, name, template);

          DA.SetData(_View_, viewPlan);
          return untracked ? null : viewPlan;
        }
      );
    }

    bool Reuse(ARDB.ViewPlan viewPlan, ARDB.Level level, ARDB.ViewFamilyType type, string name)
    {
      if (viewPlan is null) return false;
      if (!viewPlan.GenLevel.IsEquivalent(level)) return false;
      if (type.Id != viewPlan.GetTypeId()) viewPlan.ChangeTypeId(type.Id);

      return true;
    }

    ARDB.ViewPlan Create(ARDB.Level level, ARDB.ViewFamilyType type)
    {
      return ARDB.ViewPlan.Create(level.Document, type.Id, level.Id);
    }

    ARDB.ViewPlan Reconstruct(ARDB.ViewPlan viewPlan, ARDB.Level level, ARDB.ViewFamilyType type, string name, ARDB.ViewPlan template)
    {
      if (!Reuse(viewPlan, level, type, name))
        viewPlan = Create(level, type);

      viewPlan.CopyParametersFrom(template, ExcludeUniqueProperties);
      if (name is object) viewPlan?.get_Parameter(ARDB.BuiltInParameter.VIEW_NAME).Update(name);

      return viewPlan;
    }
  }

  [ComponentVersion(introduced: "1.7")]
  public class AddFloorPlan : AddViewPlan
  {
    public override Guid ComponentGuid => new Guid("3896729D-DBDF-4542-BFCF-FEC1BEBEA536");

    public AddFloorPlan() : base
    (
      name: "Add Floor Plan",
      nickname: "FloorPlan",
      description: "Given a level, it adds a floor plan to the active Revit document",
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
          Description = "Reference level for the new plan view",
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
        new Parameters.ViewFamilyType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Plan view type",
          Optional = true,
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.FloorPlan()
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
        new Parameters.FloorPlan()
        {
          Name = _View_,
          NickName = _View_.Substring(0, 1),
          Description = $"Output {_View_}",
        }
      )
    };

    protected override ARDB.ElementTypeGroup ElementTypeGroup => ARDB.ElementTypeGroup.ViewTypeFloorPlan;
    protected override ARDB.ViewType ViewType => ARDB.ViewType.FloorPlan;
  }

  [ComponentVersion(introduced: "1.7")]
  public class AddCeilingPlan : AddViewPlan
  {
    public override Guid ComponentGuid => new Guid("782D0460-8F5F-4B0E-B3B4-A1AF74BEDA6F");

    public AddCeilingPlan() : base
    (
      name: "Add Ceiling Plan",
      nickname: "CeilingPlan",
      description: "Given a level, it adds a ceiling plan to the active Revit document",
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
          Description = "Reference level for the new plan view",
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
        new Parameters.ViewFamilyType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Plan view type",
          Optional = true,
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.CeilingPlan()
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
        new Parameters.CeilingPlan()
        {
          Name = _View_,
          NickName = _View_.Substring(0, 1),
          Description = $"Output {_View_}",
        }
      )
    };

    protected override ARDB.ElementTypeGroup ElementTypeGroup => ARDB.ElementTypeGroup.ViewTypeCeilingPlan;
    protected override ARDB.ViewType ViewType => ARDB.ViewType.CeilingPlan;
  }

  [ComponentVersion(introduced: "1.7")]
  public class AddStructuralPlan : AddViewPlan
  {
    public override Guid ComponentGuid => new Guid("51F9E551-54A4-43DB-BCF3-45DF5A6F88D6");

    public AddStructuralPlan() : base
    (
      name: "Add Structural Plan",
      nickname: "StructuralPlan",
      description: "Given a level, it adds a structural plan to the active Revit document",
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
          Description = "Reference level for the new plan view",
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
        new Parameters.ViewFamilyType()
        {
          Name = "Type",
          NickName = "T",
          Description = "Plan view type",
          Optional = true,
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.StructuralPlan()
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
        new Parameters.StructuralPlan()
        {
          Name = _View_,
          NickName = _View_.Substring(0, 1),
          Description = $"Output {_View_}",
        }
      )
    };

    protected override ARDB.ElementTypeGroup ElementTypeGroup => ARDB.ElementTypeGroup.ViewTypeStructuralPlan;
    protected override ARDB.ViewType ViewType => ARDB.ViewType.EngineeringPlan;
  }
}
