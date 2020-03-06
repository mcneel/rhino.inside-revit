using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class View : Element
  {
    public override string TypeName => "Revit View";
    public override string TypeDescription => "Represents a Revit view";
    protected override Type ScriptVariableType => typeof(DB.View);
    public static explicit operator DB.View(View self) =>
      self.Document?.GetElement(self) as DB.View;

    public View() { }
    public View(DB.View view) : base(view) { }
  }
}
