using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Opening")]

  public class Opening : InstanceElement
  {
    protected override Type ValueType => typeof(ARDB.Opening);
    public new ARDB.Opening Value => base.Value as ARDB.Opening;

    public Opening() { }
    public Opening(ARDB.Opening host) : base(host) { }
  }
}
