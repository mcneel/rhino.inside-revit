using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types.Elements.View
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

    public override string DisplayName
    {
      get
      {
        var element = (DB.View) this;
        if (element is object && !string.IsNullOrEmpty(element.Title))
          return element.Title;

        return base.DisplayName;
      }
    }
  }
}
