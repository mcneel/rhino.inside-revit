using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Phase")]
  public class Phase : Element
  {
    protected override Type ScriptVariableType => typeof(DB.Phase);
    public new DB.Phase Value => base.Value as DB.Phase;

    public Phase() { }
    public Phase(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public Phase(DB.Phase value) : base(value) { }

  }
}
