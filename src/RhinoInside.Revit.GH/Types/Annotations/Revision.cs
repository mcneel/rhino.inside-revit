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
  }
}
