using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Filters
{
  using ElementTracking;
  using External.DB.Extensions;

  public class ParameterFilterElementByName : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("01E86D7C-B143-47F6-BC26-0A234EB360F3");
    public override GH_Exposure Exposure => GH_Exposure.septenary;

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.Filters);
      Menu_AppendItem
      (
        menu, $"Edit Filters…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
    }
    #endregion

    public ParameterFilterElementByName() : base
    (
      name: "Add Parameter Filter",
      nickname: "ParaFilt",
      description: "Create a parameter based filter",
      category: "Revit",
      subCategory: "Filter"
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
          Description = "Selection filter name",
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
        ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = _ParameterFilter_,
          NickName = _ParameterFilter_.Substring(0, 1),
          Description = $"Output {_ParameterFilter_}",
        }
      ),
    };

    const string _ParameterFilter_ = "Parameter Filter";
    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.ParameterFilterElement>
      (
        doc.Value, _ParameterFilter_, (ruleFilter) =>
        {
          // Input
          if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return null;
          if (!Params.TryGetDataList(DA, "Categories", out IList<Types.Category> categories)) return null;
          if (!Params.TryGetData(DA, "Filter", out Types.ElementFilter filter)) return null;

          // Compute
          StartTransaction(doc.Value);
          if (CanReconstruct(_ParameterFilter_, out var untracked, ref ruleFilter, doc.Value, name))
          {
            var categoryIds = categories?.Where(x => doc.Value.IsEquivalent(x.Document)).Select(x => x.Id).ToList();
            ruleFilter = Reconstruct(ruleFilter, doc.Value, name, categoryIds, filter?.Value, default);
          }

          DA.SetData(_ParameterFilter_, ruleFilter);
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
      else ruleFilter.SetIncrementalNomen(template?.Name ?? _ParameterFilter_);
      if (categoryIds is object) ruleFilter.SetCategories(categoryIds);
      if (filter is object) ruleFilter.SetElementFilter(filter);
      else ruleFilter.ClearRules();
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
      {
        name = doc.NextIncrementalNomen
        (
          name ?? template?.Name ?? _ParameterFilter_,
          typeof(ARDB.ParameterFilterElement)
        );
      }

      // Try to duplicate template
      if (template is object)
      {
        var ids = ARDB.ElementTransformUtils.CopyElements
        (
          template.Document,
          new ARDB.ElementId[] { template.Id },
          doc, default, default
        );

        ruleFilter = ids.Select(x => doc.GetElement(x)).OfType<ARDB.ParameterFilterElement>().FirstOrDefault();
        ruleFilter.Name = name;
      }

      if (ruleFilter is null)
      {
        ruleFilter = ARDB.ParameterFilterElement.Create
        (
          doc, name, categoryIds
        );

        if(filter is object)
          ruleFilter.SetElementFilter(filter);
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
      if (!Reuse(ruleFilter, name, categoryIds, filter, template))
      {
        ruleFilter = ruleFilter.ReplaceElement
        (
          Create(doc, name, categoryIds, filter, template),
          default
        );
      }

      return ruleFilter;
    }
  }
}
