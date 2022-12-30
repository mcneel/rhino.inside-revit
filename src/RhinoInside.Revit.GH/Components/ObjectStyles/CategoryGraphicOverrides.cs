using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.11")]
  public class CategoryGraphicOverrides : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("D296F72F-E6A8-46E0-9337-A43BA79836E6");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    protected override string IconTag => "O";

    public CategoryGraphicOverrides() : base
    (
      name: "Category Graphic Overrides",
      nickname: "CG-Overrides",
      description: "Get-Set category graphics overrides on the specified View",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View to query category graphics overrides",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Category()
        {
          Name = "Category",
          NickName = "C",
          Description = "Category to access graphics overrides",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Hidden",
          NickName = "H",
          Description = "Element Hidden state",
          Access = GH_ParamAccess.list,
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.OverrideGraphicSettings()
        {
          Name = "Overrides",
          NickName = "O",
          Description = "Element graphic overrides",
          Access = GH_ParamAccess.list,
          Optional = true
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View to query category graphic overrides",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Category()
        {
          Name = "Category",
          NickName = "C",
          Description = "Category to access graphic overrides",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Hidden",
          NickName = "H",
          Description = "Element Hidden state",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.OverrideGraphicSettings()
        {
          Name = "Overrides",
          NickName = "O",
          Description = "Element graphic overrides",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;
      else Params.TrySetData(DA, "View", () => view);

      if (!Params.GetDataList(DA, "Category", out IList<Types.Category> categories)) return;
      else Params.TrySetDataList(DA, "Category", () => categories);

      if (Params.GetDataList(DA, "Hidden", out IList<bool?> hidden) && hidden.Count > 0)
      {
        if (view.Value.AreGraphicsOverridesAllowed())
        {
          var categoriesToHide = new HashSet<ARDB.ElementId>(categories.Count);
          var categoriesToUnhide = new HashSet<ARDB.ElementId>(categories.Count);

          foreach (var pair in categories.ZipOrLast(hidden, (Category, Hidden) => (Category, Hidden)))
          {
            if (!pair.Hidden.HasValue) continue;
            if (!view.Document.IsEquivalent(pair.Category?.Document)) continue;
            if (pair.Category?.IsValid != true) continue;
            if (!view.Value.CanCategoryBeHidden(pair.Category.Id))
            {
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Category '{pair.Category.Nomen}' can not be hidden on view '{view.Value.Title}'.");
              continue;
            }

            if (pair.Hidden.Value)
            {
              categoriesToUnhide.Remove(pair.Category.Id);
              categoriesToHide.Add(pair.Category.Id);
            }
            else
            {
              categoriesToHide.Remove(pair.Category.Id);
              categoriesToUnhide.Add(pair.Category.Id);
            }
          }

          StartTransaction(view.Document);

          foreach(var categoryId in categoriesToHide)
            view.Value.SetCategoryHidden(categoryId, true);

          foreach (var categoryId in categoriesToUnhide)
            view.Value.SetCategoryHidden(categoryId, false);
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Graphics Overrides are not allowed on View '{view.Value.Title}'");
      }

      Params.TrySetDataList
      (
        DA, "Hidden", () => categories.Select
        (
          x => view.Document.IsEquivalent(x?.Document) && x.Id is ARDB.ElementId categoryId ?
               view.Value.GetCategoryHidden(categoryId) :
               default(bool?)
        )
      );

      if (Params.GetDataList(DA, "Overrides", out IList<Types.OverrideGraphicSettings> overrides) && overrides.Count > 0)
      {
        if (view.Value.AreGraphicsOverridesAllowed())
        {
          StartTransaction(view.Document);

          foreach (var pair in categories.ZipOrLast(overrides, (Category, Overrides) => (Category, Overrides)))
          {
            if (pair.Overrides?.Value is null) continue;
            if (!view.Document.IsEquivalent(pair.Category?.Document)) continue;
            if (pair.Category?.IsValid != true) continue;
            if (!view.Value.IsCategoryOverridable(pair.Category.Id))
            {
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Graphics Overrides are not allowed for Category '{pair.Category.Nomen}' on view '{view.Value.Title}'.");
              continue;
            }

            var settings = pair.Overrides.Document.IsEquivalent(view.Document) ? pair.Overrides :
                           new Types.OverrideGraphicSettings(view.Document, pair.Overrides);

            view.Value.SetCategoryOverrides(pair.Category.Id, settings.Value);
          }
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Graphics Overrides are not allowed on View '{view.Value.Title}'");
      }

      Params.TrySetDataList
      (
        DA, "Overrides", () => categories.Select
        (
          x => view.Document.IsEquivalent(x?.Document) && x?.Id is ARDB.ElementId categoryId &&
               view.Value.GetCategoryOverrides(categoryId) is ARDB.OverrideGraphicSettings overrideSettings ?
               new Types.OverrideGraphicSettings(x.Document, overrideSettings) :
               default
        )
      );
    }
  }
}
