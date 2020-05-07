using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class Panel : GraphicalElement
  {
    public override string TypeDescription => "Represents a Revit Curtain Grid Panel Element";
    protected override Type ScriptVariableType => typeof(DB.Mullion);
    public static explicit operator DB.Panel(Panel self) =>
      self.Document?.GetElement(self) as DB.Panel;

    public Panel() { }
    public Panel(DB.Panel value) : base(value) { }
  }
}
