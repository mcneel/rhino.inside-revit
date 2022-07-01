using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.8")]
  public class AddRevision : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("221E53A6-54A2-45FB-82B1-220D6E5BE884");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public AddRevision() : base
    (
      name: "Add Revision",
      nickname: "Revision",
      description: "Adds a revision at the end of the sequence of existing revisions into the active Revit document",
      category: "Revit",
      subCategory: "Annotation"
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
        new Param_Boolean()
        {
          Name = "Issued",
          NickName = "I",
          Description = "Indicates whether this Revision has been issued.",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Issued by",
          NickName = "IB",
          Description = "Indicates who has issued or will issue this Revision.",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Issued to",
          NickName = "IT",
          Description = "Indicates to whom this Revision was or will be issued.",
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
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Integer()
        {
          Name = "Revision Sequence",
          NickName = "RS",
          Description = "Revision sequence.",
        }, ParamRelevance.Primary
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
          if (!Params.TryGetData(DA, "Issued", out bool? issued)) return null;
          if (!Params.TryGetData(DA, "Issued by", out string issuedBy)) return null;
          if (!Params.TryGetData(DA, "Issued to", out string issuedTo)) return null;

          // Compute
          revision = Reconstruct
          (
            revision,
            doc.Value,
            description, date,
            issued,
            issuedBy, issuedTo,
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
      bool? issued,
      string issuedBy,
      string issuedTo,
      ARDB.RevisionVisibility? visibility
    )
    {
      if (revision is null)
        revision = ARDB.Revision.Create(document);

      if (description is object && description != revision.Description)
        revision.Description = description;

      if (date is object && date != revision.RevisionDate)
        revision.RevisionDate = date;

      if (issued is object && issued.Value != revision.Issued)
        revision.Issued = issued.Value;

      if (issuedBy is object && issuedBy != revision.IssuedBy)
        revision.IssuedBy = issuedBy;

      if (issuedTo is object && issuedTo != revision.IssuedTo)
        revision.IssuedTo = issuedTo;

      if (visibility is object && visibility.Value != revision.Visibility)
        revision.Visibility = visibility.Value;

      return revision;
    }
  }
}
