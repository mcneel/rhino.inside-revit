using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Ceiling")]
  public sealed class Ceiling : CeilingAndFloor
  {
    protected override Type ValueType => typeof(ARDB.Ceiling);
    public new ARDB.Ceiling Value => base.Value as ARDB.Ceiling;

    public Ceiling() { }
    public Ceiling(ARDB.Ceiling ceiling) : base(ceiling) { }

    public override double? LevelOffset =>
      Value?.get_Parameter(ARDB.BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM).AsDouble() * Revit.ModelUnits;
  }
}
