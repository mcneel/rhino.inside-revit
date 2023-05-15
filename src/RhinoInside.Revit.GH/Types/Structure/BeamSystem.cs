using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Beam System")]
  public class BeamSystem : InstanceElement
  {
    protected override Type ValueType => typeof(ARDB.BeamSystem);
    public new ARDB.BeamSystem Value => base.Value as ARDB.BeamSystem;

    public BeamSystem() { }
    public BeamSystem(ARDB.BeamSystem beamSystem) : base(beamSystem) { }

    public double? LevelOffset =>
      Value?.get_Parameter(ARDB.BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM).AsDouble() * Revit.ModelUnits;
  }
}
