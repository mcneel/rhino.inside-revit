using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public interface IGH_View : IGH_Element
  {
    DB.View APIView { get; }
  }

  public class View : Element, IGH_View
  {
    public override string TypeDescription => "Represents a Revit view";
    protected override Type ScriptVariableType => typeof(DB.View);
    public DB.View APIView => IsValid ? Document.GetElement(Id) as DB.View : default;
    public static explicit operator DB.View(View value) => value?.APIView;

    public View() { }
    public View(DB.View view) : base(view) { }

    public override string DisplayName
    {
      get
      {
        if(APIView is DB.View view && !string.IsNullOrEmpty(view.Title))
          return view.Title;

        return base.DisplayName;
      }
    }
  }
}
