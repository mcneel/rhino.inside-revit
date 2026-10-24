using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class PhaseExtensions
  {
    public static Phase GetPhase(this PhaseArray phases, int index) =>
      index >= 0 && index < phases.Size ? phases.get_Item(index) : null;

    public static int SequenceIndex(this Phase phase) =>
      phase.get_Parameter(BuiltInParameter.PHASE_SEQUENCE_NUMBER).AsInteger() - 1;
  }
}
