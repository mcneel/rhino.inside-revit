using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Documents
{
  [ComponentVersion(introduced: "1.10")]
  public class DocumentWarnings : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("3917ADB2-706E-49A2-A3AF-6B5F610C4B78");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override string IconTag => "⚠";

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.ReviewWarnings);
      Menu_AppendItem
      (
        menu, $"Review Warnings…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
    }
    #endregion


    public DocumentWarnings() : base
    (
      name: "Document Warnings",
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
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Param_GenericObject()
        {
          Name = "Failure Definition",
          NickName = "FD",
          Description = "Failure definition.",
          Access = GH_ParamAccess.list,
          DataMapping = GH_DataMapping.Graft,
          Hidden = true,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Description",
          NickName = "D",
          Description = "Failure definition description.",
          Access = GH_ParamAccess.list,
          DataMapping = GH_DataMapping.Graft
        }, ParamRelevance.Primary
      ),
      ParamDefinition.Create<Parameters.Element>("Failing Elements", "FE", "List of elements that caused the failure.", access: GH_ParamAccess.tree, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.Element>("Additional Elements", "AE", "List of additional reference elements for the failure.", access: GH_ParamAccess.tree, relevance: ParamRelevance.Primary),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;
      else Params.TrySetData(DA, "Document", () => doc);

      var warnings = doc.Value.GetWarnings();

      Params.TrySetDataList(DA, "Failure Definition", () => warnings.Select(x => new Types.FailureDefinition(x.GetFailureDefinitionId().Guid)));
      Params.TrySetDataList(DA, "Description", () => warnings.Select(x => x.GetDescriptionText()));

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
    }
  }
}
