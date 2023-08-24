using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;
using OS = System.Environment;

namespace RhinoInside.Revit.GH.Components.Documents
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.10", updated: "1.17"), ComponentRevitAPIVersion(min: "2018.0")]
  public class DocumentWarnings : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("3917ADB2-706E-49A2-A3AF-6B5F610C4B78");
    public override GH_Exposure Exposure => SDKCompliancy(GH_Exposure.primary | GH_Exposure.obscure);

    protected override string IconTag => "⚠";

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);
      menu.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.ReviewWarnings, "Review Warnings…");
    }
    #endregion

    public DocumentWarnings() : base
    (
      name: "Query Warnings",
      nickname: "Warnings",
      description: "Gets a list of failure messages generated from persistent (reviewable) warnings accumulated in the document.",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Param_GenericObject()
        {
          Name = "Failure Definitions",
          NickName = "FD",
          Description = "Set of failure definitions to query.",
          Access = GH_ParamAccess.list,
          Hidden = true,
          Optional = true,
        }, ParamRelevance.Primary
      ),
      ParamDefinition.Create<Parameters.Element>("Failing Element", "FE", "Element that caused the failure.", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.Element>("Additional Elements", "AE", "List of additional reference elements for the failure.", optional: true, access: GH_ParamAccess.list, relevance: ParamRelevance.Secondary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Param_GenericObject()
        {
          Name = "Failure Definitions",
          NickName = "FD",
          Description = "Failure definition.",
          Access = GH_ParamAccess.list,
          DataMapping = GH_DataMapping.Graft,
          Hidden = true,
        }
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Descriptions",
          NickName = "D",
          Description = "Failure definition description.",
          Access = GH_ParamAccess.list,
          DataMapping = GH_DataMapping.Graft
        }, ParamRelevance.Occasional
      ),
      ParamDefinition.Create<Parameters.Element>("Failing Elements", "FE", "List of elements that caused the failure.", access: GH_ParamAccess.tree, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.Element>("Additional Elements", "AE", "List of additional reference elements for the failure.", access: GH_ParamAccess.tree, relevance: ParamRelevance.Secondary),
    };

    public override void AddedToDocument(GH_Document document)
    {
      if (Params.Output<IGH_Param>("Failure Definition") is IGH_Param failureDefinition)
        failureDefinition.Name = "Failure Definitions";

      if (Params.Output<IGH_Param>("Description") is IGH_Param description)
        description.Name = "Descriptions";

      base.AddedToDocument(document);
    }

    static string GetDescriptionText(ARDB.FailureMessage message)
    {
      if (message?.GetDescriptionText() is string description)
      {
        if (description != string.Empty)
        {
          description = description.Trim();
          description = description.Replace("...", "…");
          var lines = description.Split(new string[] { ". ", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
          description = string.Join
          (
            OS.NewLine,
            lines.Select
            (
              x =>
              {
                var line = x.Trim();
                if (line != string.Empty && !char.IsPunctuation(line[line.Length - 1]))
                  line += '.';

                return line;
              }
            )
          );
        }

        return description;
      }

      return null;
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;
      else Params.TrySetData(DA, "Document", () => doc);

      if (!Params.TryGetDataList(DA, "Failure Definitions", out IList<Types.FailureDefinition> failureDefinitions)) return;
      if (!Params.TryGetData(DA, "Failing Element", out Types.Element element)) return;
      if (!Params.TryGetData(DA, "Additional Elements", out IList<Types.Element> additionals)) return;

#if REVIT_2018
      var warnings = doc.Value.GetWarnings();

      if (failureDefinitions is object)
      {
        var definitions = new HashSet<Guid>(failureDefinitions.Select(x => x.Value));
        warnings = warnings.Where(x => definitions.Contains(x.GetFailureDefinitionId().Guid)).ToArray();
      }

      if (element is object)
      {
        var elementId = element.Id;
        warnings = warnings.Where(x => x.GetFailingElements().ToReadOnlyElementIdCollection().Contains(elementId)).ToArray();
      }

      if (additionals is object && additionals.Count > 0)
      {
        var additional = new HashSet<ARDB.ElementId>(additionals.Where(x => doc.Value.IsEquivalent(x?.Document)).Select(x => x.Id));
        warnings = warnings.Where(x => additional.Overlaps(x.GetAdditionalElements())).ToArray();
      }

      Params.TrySetDataList(DA, "Failure Definitions", () => warnings.Select(x => new Types.FailureDefinition(x.GetFailureDefinitionId().Guid)));
      Params.TrySetDataList(DA, "Descriptions", () => warnings.Select(GetDescriptionText));

      var _FailingElements_ = Params.IndexOfOutputParam("Failing Elements");
      if (_FailingElements_ != -1)
      {
        var failingElementsPath = DA.ParameterTargetPath(_FailingElements_).AppendElement(DA.ParameterTargetIndex(_FailingElements_));
        var failingElements = new GH_Structure<Types.Element>();

        var index = 0;
        foreach (var warning in warnings)
        {
          failingElements.EnsurePath(failingElementsPath.AppendElement(index++));
          failingElements.AppendRange(warning.GetFailingElements().Select(x => Types.Element.FromElementId(doc.Value, x)));
        }

        DA.SetDataTree(_FailingElements_, failingElements);
      }

      var _AdditionalElements_ = Params.IndexOfOutputParam("Additional Elements");
      if (_AdditionalElements_ != -1)
      {
        var additionalElementsPath = DA.ParameterTargetPath(_AdditionalElements_).AppendElement(DA.ParameterTargetIndex(_AdditionalElements_));
        var additionalElements = new GH_Structure<Types.Element>();

        var index = 0;
        foreach (var warning in warnings)
        {
          additionalElements.EnsurePath(additionalElementsPath.AppendElement(index++));
          additionalElements.AppendRange(warning.GetAdditionalElements().Select(x => Types.Element.FromElementId(doc.Value, x)));
        }

        DA.SetDataTree(_AdditionalElements_, additionalElements);
      }
#endif
    }
  }
}
