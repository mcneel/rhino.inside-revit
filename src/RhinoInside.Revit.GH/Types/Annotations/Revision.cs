using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Revision")]
  public class Revision : Element
  {
    protected override Type ValueType => typeof(ARDB.Revision);
    public new ARDB.Revision Value => base.Value as ARDB.Revision;

    public Revision() { }
    public Revision(ARDB.Revision element) : base(element) { }

    public string RevisionNumber => Value?.get_Parameter(ARDB.BuiltInParameter.PROJECT_REVISION_REVISION_NUM)?.AsString();
    public int? SequenceNumber => Value?.get_Parameter(ARDB.BuiltInParameter.PROJECT_REVISION_SEQUENCE_NUM)?.AsInteger();

    public string Description
    {
      get => Value?.Description;
      set { if (value is object && Value is ARDB.Revision revision && revision.Description != value) revision.Description = value; }
    }

    public string RevisionDate
    {
      get => Value?.RevisionDate;
      set { if (value is object && Value is ARDB.Revision revision && revision.RevisionDate != value) revision.RevisionDate = value; }
    }

    public string IssuedBy
    {
      get => Value?.IssuedBy;
      set { if (value is object && Value is ARDB.Revision revision && revision.IssuedBy != value) revision.IssuedBy = value; }
    }

    public string IssuedTo
    {
      get => Value?.IssuedTo;
      set { if (value is object && Value is ARDB.Revision revision && revision.IssuedTo != value) revision.IssuedTo = value; }
    }
    public bool? Issued
    {
      get => Value?.Issued;
      set { if (value is object && Value is ARDB.Revision revision && revision.Issued != value) revision.Issued = value.Value; }
    }
  }
}
