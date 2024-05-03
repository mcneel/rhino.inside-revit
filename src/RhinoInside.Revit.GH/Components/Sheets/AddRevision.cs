using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Sheets
{
  [ComponentVersion(introduced: "1.8", updated: "1.21")]
  public class AddRevision : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("221E53A6-54A2-45FB-82B1-220D6E5BE884");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => string.Empty;

    public AddRevision() : base
    (
      name: "Add Revision",
      nickname: "Revision",
      description: "Adds a revision at the end of the sequence of existing revisions into the active Revit document",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Document",
          Optional = true
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Revision Date",
          NickName = "RD",
          Description = "Revision date.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Revision Description",
          NickName = "RDN",
          Description = "Revision description.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Issued by",
          NickName = "IB",
          Description = "Indicates who has issued or will issue this revision.",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Issued to",
          NickName = "IT",
          Description = "Indicates to whom this revision was or will be issued.",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Issued",
          NickName = "I",
          Description = "Indicates whether this revision has been issued.",
          Optional = true
        }, ParamRelevance.Secondary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Revision()
        {
          Name = _Revision_,
          NickName = _Revision_.Substring(0, 1),
          Description = $"Output {_Revision_}",
        }
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Revision Number",
          NickName = "RN",
          Description = "Revision number.",
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Integer()
        {
          Name = "Revision Sequence",
          NickName = "RS",
          Description = "Revision sequence.",
        }, ParamRelevance.Occasional
      ),
    };

    const string _Revision_ = "Revision";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.Revision>
      (
        doc.Value, _Revision_, revision =>
        {
          // Input
          if (!Params.TryGetData(DA, "Revision Date", out string date)) return null;
          if (!Params.TryGetData(DA, "Revision Description", out string description)) return null;
          if (!Params.TryGetData(DA, "Issued by", out string issuedBy)) return null;
          if (!Params.TryGetData(DA, "Issued to", out string issuedTo)) return null;
          if (!Params.TryGetData(DA, "Issued", out bool? issued)) return null;

          // Compute
          revision = Reconstruct
          (
            revision,
            doc.Value,
            description, date,
            issuedBy, issuedTo,
            issued,
            ARDB.RevisionVisibility.CloudAndTagVisible
          );

          DA.SetData(_Revision_, revision);
          Params.TrySetData(DA, "Revision Number", () => revision.get_Parameter(ARDB.BuiltInParameter.PROJECT_REVISION_REVISION_NUM)?.AsString());
          Params.TrySetData(DA, "Revision Sequence", () => revision.get_Parameter(ARDB.BuiltInParameter.PROJECT_REVISION_SEQUENCE_NUM)?.AsInteger());
          return revision;
        }
      );
    }

    ARDB.Revision Reconstruct
    (
      ARDB.Revision revision,
      ARDB.Document document,
      string description,
      string date,
      string issuedBy,
      string issuedTo,
      bool? issued,
      ARDB.RevisionVisibility? visibility
    )
    {
      if (revision is null)
        revision = ARDB.Revision.Create(document);

      RevisionIssue.Invoke(this, revision, description, date, issuedBy, issuedTo, issued);

      if (visibility is object && visibility.Value != revision.Visibility)
        revision.Visibility = visibility.Value;

      return revision;
    }
  }

  [ComponentVersion(introduced: "1.21")]
  public class RevisionIssue : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("504CB82C-FA96-4506-8F3B-6ADE2DFFF6F0");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public RevisionIssue()
    : base
    (
      name: "Revision Issue",
      nickname: "R-Issue",
      description: "Get-Set revision issue status.",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.Revision>("Revision", "R"),

      ParamDefinition.Create<Param_String>("Revision Date", "RD", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Revision Description", "RDN", optional: true, relevance: ParamRelevance.Primary),

      ParamDefinition.Create<Param_String>("Issued By", "IB", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Issued To", "IT", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Boolean>("Issued", "I", optional: true, relevance: ParamRelevance.Primary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Revision>("Revision", "R", relevance: ParamRelevance.Occasional),

      ParamDefinition.Create<Param_String>("Revision Date", "RD", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Revision Description", "RDN", relevance: ParamRelevance.Primary),

      ParamDefinition.Create<Param_String>("Issued By", "IB", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Issued To", "IT", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Boolean>("Issued", "I", relevance: ParamRelevance.Primary),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Revision", out Types.Revision revision, x => x.IsValid))
        return;

      bool update = false;
      update |= Params.GetData(DA, "Revision Date", out string date);
      update |= Params.GetData(DA, "Revision Description", out string description);
      update |= Params.GetData(DA, "Issued By", out string issuedBy);
      update |= Params.GetData(DA, "Issued To", out string issuedTo);
      update |= Params.GetData(DA, "Issued", out bool? issued);

      if (update) Invoke(this, revision.Value, date, description, issuedBy, issuedTo, issued);

      Params.TrySetData(DA, "Revision", () => revision);

      Params.TrySetData(DA, "Revision Date", () => revision.RevisionDate);
      Params.TrySetData(DA, "Revision Description", () => revision.Description);

      Params.TrySetData(DA, "Issued By", () => revision.IssuedBy);
      Params.TrySetData(DA, "Issued To", () => revision.IssuedTo);
      Params.TrySetData(DA, "Issued", () => revision.Issued);
    }

    internal static void Invoke(TransactionalChainComponent component, ARDB.Revision revision, string date, string description, string issuedBy, string issuedTo, bool? issued)
    {
      var updateDate = date is object && revision.RevisionDate != date;
      var updateDescription = description is object && revision.Description != description;
      var updateIssuedBy = issuedBy is object && revision.IssuedBy != issuedBy;
      var updateIssuedTo = issuedTo is object && revision.IssuedTo != issuedTo;
      var updateIssued = issued is object && revision.Issued != issued;

      if (updateDate || updateDescription || updateIssuedBy || updateIssuedTo || updateIssued)
      {
        component.StartTransaction(revision.Document);

        issued = issued ?? revision.Issued;
        if (issued is false && revision.Issued) revision.Issued = false;

        if (revision.Issued)
        {
          switch (component.FailureProcessingMode)
          {
            case ARDB.FailureProcessingResult.Continue:
              if (updateDate) component.AddContinuableFailure($"Can't set 'Revision Date' parameter. Revision '{revision.Description}' is already issued. {{{revision.Id}}}");
              if (updateDescription) component.AddContinuableFailure($"Can't set 'Revision Description' parameter. Revision '{revision.Description}' is already issued. {{{revision.Id}}}");
              if (updateIssuedBy) component.AddContinuableFailure($"Can't set 'Issued By' parameter. Revision '{revision.Description}' is already issued. {{{revision.Id}}}");
              if (updateIssuedTo) component.AddContinuableFailure($"Can't set 'Issued To' parameter. Revision '{revision.Description}' is already issued. {{{revision.Id}}}");
              return;

            case ARDB.FailureProcessingResult.ProceedWithCommit:
              revision.Issued = false;
              issued = true;
              updateIssued = true;
              component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Updating the already issued revision '{revision.Description}'. {{{revision.Id}}}");
              break;

            case ARDB.FailureProcessingResult.WaitForUserInput:
              using (var failure = new ARDB.FailureMessage(ARDB.BuiltInFailures.GeneralFailures.CannotSetParameter))
              {
                failure.SetFailingElement(revision.Id);
                revision.Document.PostFailure(failure);
              }
              break;

            default:
              throw new Exceptions.RuntimeException($"Can't set the parameter. Revision '{revision.Description}' is already issued. {{{revision.Id}}}");
          }
        }
        if (updateDate) revision.RevisionDate = date;
        if (updateDescription) revision.Description = description;
        if (updateIssuedBy) revision.IssuedBy = issuedBy;
        if (updateIssuedTo) revision.IssuedTo = issuedTo;
        if (updateIssued) revision.Issued = issued.Value;
      }
    }
  }

}
