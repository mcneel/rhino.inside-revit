using System;
using Grasshopper.Kernel.Types;
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

    public override bool CastFrom(object source)
    {
      var value = source;

      if (source is IGH_Goo goo)
        value = goo.ScriptVariable();

      if (value is ARDB.View view)
      {
        if (view.get_Parameter(ARDB.BuiltInParameter.VIEW_PHASE) is ARDB.Parameter viewPhase)
          SetValue(view.Document, viewPhase.AsElementId());
        else
          SetValue(default, ARDB.ElementId.InvalidElementId);

        return true;
      }
      else if (value is ARDB.Element element)
      {
        SetValue(element.Document, element.CreatedPhaseId);
        return true;
      }

      return base.CastFrom(source);
    }
  }
}
