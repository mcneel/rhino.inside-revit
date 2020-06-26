using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class Panel : FamilyInstance
  {
    public override string TypeName => "Revit Panel";

    public override string TypeDescription => "Represents a Revit Curtain Grid Panel Element";
    protected override Type ScriptVariableType => typeof(DB.FamilyInstance);
    public static explicit operator DB.FamilyInstance(Panel value) =>
      value.IsValid ? value.Document?.GetElement(value) as DB.FamilyInstance : default;

    protected override bool SetValue(DB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(DB.Element element)
    {
      return element is DB.FamilyInstance instance && instance.Symbol.Family.IsCurtainPanelFamily;
    }

    public Panel() { }
    public Panel(DB.FamilyInstance value) : base(value) { }
  }
}
