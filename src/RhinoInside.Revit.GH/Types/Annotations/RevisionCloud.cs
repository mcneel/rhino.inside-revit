using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Revision Cloud")]
  public class RevisionCloud : GeometricElement
  {
    protected override Type ValueType => typeof(ARDB.RevisionCloud);
    public new ARDB.RevisionCloud Value => base.Value as ARDB.RevisionCloud;

    public RevisionCloud() { }
    public RevisionCloud(ARDB.RevisionCloud element) : base(element) { }
  }
}
