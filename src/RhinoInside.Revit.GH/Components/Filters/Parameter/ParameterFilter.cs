using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Filters
{
  using External.DB;
  using External.DB.Extensions;

  public class ParameterFilterElementByName : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("01E86D7C-B143-47F6-BC26-0A234EB360F3");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);
      menu.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.Filters, "Edit Filters…");
    }
    #endregion

    public ParameterFilterElementByName() : base
    (
      name: "Add Rule-based Filter",
      nickname: "RuleFilt",
      description: "Create a parameter rule-based filter",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document() { Optional = true }, ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Filter name",
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Category()
        {
          Name = "Categories",
          NickName = "C",
          Description = "Categories",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.ElementFilter()
        {
          Name = "Filter",
          NickName = "F",
          Description = "Filter",
          Access = GH_ParamAccess.item,
          Optional = true
        },
#if REVIT_2019
        ParamRelevance.Primary
#else
        ParamRelevance.Occasional
#endif
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.FilterElement()
        {
          Name = _RuleBasedFilter_,
          NickName = _RuleBasedFilter_.Substring(0, 1),
          Description = $"Output {_RuleBasedFilter_}",
        }
      ),
    };

    public override void AddedToDocument(GH_Document document)
    {
      // V 1.12
      if (Params.Output<IGH_Param>("Parameter Filter") is IGH_Param parameterFilter) parameterFilter.Name = "Rule-based Filter";

      base.AddedToDocument(document);
    }

    const string _RuleBasedFilter_ = "Rule-based Filter";
    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.ParameterFilterElement>
      (
        doc.Value, _RuleBasedFilter_, ruleFilter =>
        {
          // Input
          if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return null;
          if (!Params.TryGetDataList(DA, "Categories", out IList<Types.Category> categories)) return null;
          if (!Params.TryGetData(DA, "Filter", out Types.ElementFilter filter)) return null;

          // Compute
          StartTransaction(doc.Value);
          if (CanReconstruct(_RuleBasedFilter_, out var untracked, ref ruleFilter, doc.Value, name))
          {
            var categoryIds = categories?.Where(x => doc.Value.IsEquivalent(x?.Document)).Select(x => x.Id).ToHashSet();

            try { ruleFilter = Reconstruct(ruleFilter, doc.Value, name, categoryIds, filter?.Value, default); }
            catch (Exception e)
            {
              if (FailureProcessingMode == ARDB.FailureProcessingResult.Continue)
                throw new Exceptions.RuntimeException(e.Message.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)[0]);
              else if (FailureProcessingMode == ARDB.FailureProcessingResult.ProceedWithCommit)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, e.Message.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)[0]);
              else throw new Exceptions.RuntimeErrorException(e.Message.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)[0]);
            }
          }

          DA.SetData(_RuleBasedFilter_, ruleFilter);
          return untracked ? null : ruleFilter;
        }
      );
    }

    bool Reuse
    (
      ARDB.ParameterFilterElement ruleFilter,
      string name,
      ICollection<ARDB.ElementId> categoryIds,
      ARDB.ElementFilter filter,
      ARDB.ParameterFilterElement template
    )
    {
      if (ruleFilter is null) return false;
      if (name is object) { if (ruleFilter.Name != name) ruleFilter.Name = name; }
      else ruleFilter.SetIncrementalNomen(template?.Name ?? _RuleBasedFilter_);
      if (categoryIds is object) ruleFilter.SetCategories(categoryIds);

      ruleFilter.CopyParametersFrom(template);
      return true;
    }

    ARDB.ParameterFilterElement Create
    (
      ARDB.Document doc,
      string name,
      ICollection<ARDB.ElementId> categoryIds,
      ARDB.ElementFilter filter,
      ARDB.ParameterFilterElement template
    )
    {
      var ruleFilter = default(ARDB.ParameterFilterElement);

      // Make sure the name is unique
      if (name is null)
      {
        name = doc.NextIncrementalNomen
        (
          template?.Name ?? _RuleBasedFilter_, typeof(ARDB.ParameterFilterElement),
          categoryId: ARDB.BuiltInCategory.INVALID
        );
      }

      // Try to duplicate template
      if (template is object)
      {
        ruleFilter = template.CloneElement(doc);
        ruleFilter.Name = name;
      }

      if (ruleFilter is null)
      {
        ruleFilter = ARDB.ParameterFilterElement.Create
        (
          doc, name, categoryIds
        );
      }

      return ruleFilter;
    }

    ARDB.ParameterFilterElement Reconstruct
    (
      ARDB.ParameterFilterElement ruleFilter,
      ARDB.Document doc,
      string name,
      ICollection<ARDB.ElementId> categoryIds,
      ARDB.ElementFilter filter,
      ARDB.ParameterFilterElement template
    )
    {
      var inputCategoryIds = categoryIds;
      categoryIds = ARDB.ParameterFilterUtilities.RemoveUnfilterableCategories(inputCategoryIds);
      if (categoryIds.Count != inputCategoryIds.Count)
      {
        if (FailureProcessingMode != ARDB.FailureProcessingResult.ProceedWithCommit)
          throw new Exceptions.RuntimeErrorException("Input 'Categories' parameter contains unfilterable categories.");
        else foreach(var id in inputCategoryIds.Where(x => !categoryIds.Contains(x)))
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Unfilterable category '{doc.GetCategory(id)?.Name}' was not applied.");
      }

      if (!Reuse(ruleFilter, name, categoryIds, filter, template))
      {
        ruleFilter = ruleFilter.ReplaceElement
        (
          Create(doc, name, categoryIds, filter, template),
          default
        );
      }

      if (ruleFilter is object && filter is object)
      {
        if (categoryIds.Count == 0 || filter.IsEmpty()) ruleFilter.ClearRules();
        else 
        {
          if (!ruleFilter.ElementFilterIsAcceptableForParameterFilterElement(filter))
            throw new Exceptions.RuntimeErrorException
            (
#if REVIT_2019
              $"The input 'Filter' is not acceptable for use by a Rule-based Filter.\r" +
              "Only Parameter Filters or Logical combinations of these are accepted."
#else
              "Parameter Filter is partially supported before Revit 2019."
#endif
            );

          ruleFilter.SetElementFilter(filter);
        }
      }

      return ruleFilter;
    }
  }
}
