using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Floor")]
  public sealed class Floor : CeilingAndFloor
  {
    protected override Type ValueType => typeof(ARDB.Floor);
    public new ARDB.Floor Value => base.Value as ARDB.Floor;

    public Floor() { }
    public Floor(ARDB.Floor floor) : base(floor) { }

    public override double? LevelOffset =>
      Value?.get_Parameter(ARDB.BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).AsDouble() * Revit.ModelUnits;

    #region Structure
    public override bool Structural => Value?.get_Parameter(ARDB.BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL).AsBoolean() is true;
    #endregion
  }
}
