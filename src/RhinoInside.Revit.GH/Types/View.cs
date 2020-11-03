using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("View")]
  public interface IGH_View : IGH_Element { }

  [Kernel.Attributes.Name("View")]
  public class View : Element, IGH_View
  {
    protected override Type ScriptVariableType => typeof(DB.View);
    public static explicit operator DB.View(View value) => value?.Value;
    public new DB.View Value => base.Value as DB.View;

    public View() { }
    public View(DB.View view) : base(view) { }

    public override string DisplayName
    {
      get
      {
        if(Value is DB.View view && !string.IsNullOrEmpty(view.Title))
          return view.Title;

        return base.DisplayName;
      }
    }
  }
}
