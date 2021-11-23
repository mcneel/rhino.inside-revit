using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Phase")]
  public class Phase : Element
  {
    protected override Type ValueType => typeof(ARDB.Phase);
    public new ARDB.Phase Value => base.Value as ARDB.Phase;

    public Phase() { }
    public Phase(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public Phase(ARDB.Phase value) : base(value) { }

  }
}
