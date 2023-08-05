using System;
using System.Linq;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Phase")]
  public class Phase : Element
  {
    protected override Type ValueType => typeof(ARDB.Phase);
    public new ARDB.Phase Value => base.Value as ARDB.Phase;

    public Phase() { }
    public Phase(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public Phase(ARDB.Phase value) : base(value) { }

    public override bool CastFrom(object source)
    {
      var value = source;
      if (source is IGH_Goo goo)
        value = goo.ScriptVariable();

      switch (value)
      {
        case ARDB.View view:
          if (view.get_Parameter(ARDB.BuiltInParameter.VIEW_PHASE) is ARDB.Parameter viewPhase && viewPhase.HasValue)
            SetValue(view.Document, viewPhase.AsElementId());
          else
            SetValue(default, ElementIdExtension.Invalid);

          return true;

        case ARDB.SpatialElement spatialElement:
          if (spatialElement.get_Parameter(ARDB.BuiltInParameter.ROOM_PHASE) is ARDB.Parameter phase && phase.HasValue)
            SetValue(spatialElement.Document, phase.AsElementId() ?? ElementIdExtension.Invalid);
          else
            SetValue(default, ElementIdExtension.Invalid);

          return true;

        case ARDB.Mechanical.Zone zone:
          if (zone.Phase is ARDB.Phase zonePhase)
            SetValue(zonePhase);
          else
            SetValue(default, ElementIdExtension.Invalid);

          return true;

        case ARDB.Element element:
          if (element.HasPhases())
            SetValue(element.Document, element.CreatedPhaseId);
          else
            SetValue(default, ElementIdExtension.Invalid);

          return true;

        case ARDB.Document document:
          if (!document.IsFamilyDocument && document.Phases?.Cast<ARDB.Phase>().LastOrDefault() is ARDB.Phase lastPhase)
            SetValue(lastPhase);
          else
            SetValue(default, ElementIdExtension.Invalid);

          return true;
      }

      return base.CastFrom(source);
    }

    public int? SequenceNumber => Value?.get_Parameter(ARDB.BuiltInParameter.PHASE_SEQUENCE_NUMBER).AsInteger();
  }
}
