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

  public class SelectionFilterElementByName : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("29618F71-3B57-4A20-9CB2-4C3D17774172");
    public override GH_Exposure Exposure => GH_Exposure.septenary;

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.EditSelection);
      Menu_AppendItem
      (
        menu, $"Edit Selection…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
    }
    #endregion

    public SelectionFilterElementByName() : base
    (
      name: "Add Selection Filter",
      nickname: "Selection Filter",
      description: "Create a selection filter",
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
          Optional = true,
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Elements",
          NickName = "E",
          Description = "Elements",
          Access = GH_ParamAccess.list,
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
          Name = _SelectionFilter_,
          NickName = _SelectionFilter_.Substring(0, 1),
          Description = $"Output {_SelectionFilter_}",
        }
      ),
    };

    const string _SelectionFilter_ = "Selection Filter";
    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;
      if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return;
      if (!Params.TryGetDataList(DA, "Elements", out IList<Types.IGH_GraphicalElement> elements)) return;

      // Previous Output
      Params.ReadTrackedElement(_SelectionFilter_, doc.Value, out ARDB.SelectionFilterElement selection);

      StartTransaction(doc.Value);
      {
        var elementIds = elements?.Where(x => doc.Value.IsEquivalent(x.Document)).Select(x => x.Id).ToList();
        selection = Reconstruct(selection, doc.Value, name, elementIds, default);

        Params.WriteTrackedElement(_SelectionFilter_, doc.Value, selection);
        DA.SetData(_SelectionFilter_, selection);
      }
    }

    bool Reuse
    (
      ARDB.SelectionFilterElement selection,
      string name,
      ICollection<ARDB.ElementId> elementIds,
      ARDB.SelectionFilterElement template
    )
    {
      if (selection is null) return false;
      if (name is object) selection.Name = name;
      if (elementIds is object) selection.SetElementIds(elementIds);
      selection.CopyParametersFrom(template);
      return true;
    }

    ARDB.SelectionFilterElement Create
    (
      ARDB.Document doc,
      string name,
      ICollection<ARDB.ElementId> elementIds,
      ARDB.SelectionFilterElement template
    )
    {
      var selection = default(ARDB.SelectionFilterElement);

      // Make sure the name is unique
      {
        if (name is null)
          name = template?.Name ?? _SelectionFilter_;

        name = doc.GetNamesakeElements
        (
          typeof(ARDB.FilterElement), name
        ).
        Select(x => x.Name).
        WhereNamePrefixedWith(name).
        NextNameOrDefault() ?? name;
      }

      // Try to duplicate template
      if (template is object)
      {
        var ids = ARDB.ElementTransformUtils.CopyElements
        (
          template.Document,
          new ARDB.ElementId[] { template.Id },
          doc,
          default,
          default
        );

        selection = ids.Select(x => doc.GetElement(x)).OfType<ARDB.SelectionFilterElement>().FirstOrDefault();
        selection.Name = name;
      }

      if (selection is null)
        selection = ARDB.SelectionFilterElement.Create(doc, name);

      if(elementIds is object)
        selection.SetElementIds(elementIds);

      return selection;
    }

    ARDB.SelectionFilterElement Reconstruct
    (
      ARDB.SelectionFilterElement selection,
      ARDB.Document doc,
      string name,
      ICollection<ARDB.ElementId> elementIds,
      ARDB.SelectionFilterElement template
    )
    {
      if (!Reuse(selection, name, elementIds, template))
      {
        selection = selection.ReplaceElement
        (
          Create(doc, name, elementIds, template),
          default
        );
      }

      return selection;
    }
  }
}
