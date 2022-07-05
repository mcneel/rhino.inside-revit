using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  using System.Windows.Forms;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.8")]
  public class QueryRevisions : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("8EAD987D-EAF8-4E79-B33A-0E29DAF7F9BB");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => String.Empty;
    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementClassFilter(typeof(ARDB.Revision));

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var SheetIssuesOrRevisionsId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.SheetIssuesOrRevisions);
      Menu_AppendItem
      (
        menu, "Open Sheet Issues/Revisions…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, SheetIssuesOrRevisionsId),
        activeApp.CanPostCommand(SheetIssuesOrRevisionsId), false
      );
    }
    #endregion

    public QueryRevisions() : base
    (
      name: "Query Revisions",
      nickname: "Revisions",
      description: "Get all document revisions",
      category: "Revit",
      subCategory: "Annotation"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document() { Optional = true }, ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Revision Number", "RN", "Revision number", optional: true, relevance : ParamRelevance.Primary),
      ParamDefinition.Create<Param_Integer>("Revision Sequence", "RS", "Revision number", optional: true, relevance : ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Revision Date", "RD", "Revision date", optional: true, relevance : ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Revision Description", "RDN", "Revision description", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Boolean>("Issued", "I", "Whether Revision has been issued.", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Issued by", "IB", "Who has issued or will issue the revision.", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Issued to", "IT", "Whom the revision was or will be issued.", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", optional: true, relevance: ParamRelevance.Occasional),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Revision>("Revisions", "R", "Revisions list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;
      if (!Params.TryGetData(DA, "Revision Number", out string number)) return;
      if (!Params.TryGetData(DA, "Revision Sequence", out int? sequence)) return;
      if (!Params.TryGetData(DA, "Revision Date", out string date)) return;
      if (!Params.TryGetData(DA, "Revision Description", out string description)) return;
      if (!Params.TryGetData(DA, "Issued", out bool? issued)) return;
      if (!Params.TryGetData(DA, "Issued by", out string issuedBy)) return;
      if (!Params.TryGetData(DA, "Issued to", out string issuedTo)) return;
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter)) return;

      using (var collector = new ARDB.FilteredElementCollector(doc.Value))
      {
        var revisionCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          revisionCollector = revisionCollector.WherePasses(filter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.PROJECT_REVISION_REVISION_NUM, ref number, out var numberFilter))
          revisionCollector = revisionCollector.WherePasses(numberFilter);

        if (sequence.HasValue && TryGetFilterIntegerParam(ARDB.BuiltInParameter.PROJECT_REVISION_SEQUENCE_NUM, sequence.Value, out var sequenceFilter))
          revisionCollector = revisionCollector.WherePasses(sequenceFilter);
        else
        {
          revisionCollector = revisionCollector.WherePasses
          (
            new ARDB.ElementParameterFilter(new ARDB.FilterIntegerRule
            (
              new ARDB.ParameterValueProvider(new ARDB.ElementId(ARDB.BuiltInParameter.PROJECT_REVISION_SEQUENCE_NUM)),
              new ARDB.FilterNumericGreater(), 0
            ))
          );
        }

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.PROJECT_REVISION_REVISION_DATE, ref date, out var dateFilter))
          revisionCollector = revisionCollector.WherePasses(dateFilter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.PROJECT_REVISION_REVISION_DESCRIPTION, ref description, out var descriptionFilter))
          revisionCollector = revisionCollector.WherePasses(descriptionFilter);

        if (issued.HasValue && TryGetFilterIntegerParam(ARDB.BuiltInParameter.PROJECT_REVISION_REVISION_ISSUED, issued.Value ? 1:0, out var issuedFilter))
          revisionCollector = revisionCollector.WherePasses(issuedFilter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.PROJECT_REVISION_REVISION_ISSUED_BY, ref issuedBy, out var issuedByFilter))
          revisionCollector = revisionCollector.WherePasses(issuedByFilter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.PROJECT_REVISION_REVISION_ISSUED_TO, ref issuedTo, out var issuedToFilter))
          revisionCollector = revisionCollector.WherePasses(issuedToFilter);

        var revisions = collector.Cast<ARDB.Revision>();

        if (number is object)
          revisions = revisions.Where(x => x.RevisionNumber.IsSymbolNameLike(number));

        if (date is object)
          revisions = revisions.Where(x => x.RevisionDate.IsSymbolNameLike(date));

        if (description is object)
          revisions = revisions.Where(x => x.Description.IsSymbolNameLike(description));

        if (issuedBy is object)
          revisions = revisions.Where(x => x.IssuedBy.IsSymbolNameLike(issuedBy));

        if (issuedTo is object)
          revisions = revisions.Where(x => x.IssuedTo.IsSymbolNameLike(issuedTo));

        DA.SetDataList
        (
          "Revisions",
          revisions.
          Select(Types.Revision.FromElement).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
