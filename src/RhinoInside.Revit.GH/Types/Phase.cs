using System;
using System.Linq;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Phase")]
  public sealed class Phase : Element
  {
    protected override Type ValueType => typeof(ARDB.Phase);
    public new ARDB.Phase Value => base.Value as ARDB.Phase;

    public Phase() { }
    public Phase(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public Phase(ARDB.Phase value) : base(value) { }

    public int? SequenceNumber => Value?.get_Parameter(ARDB.BuiltInParameter.PHASE_SEQUENCE_NUMBER).AsInteger();
  }
}
