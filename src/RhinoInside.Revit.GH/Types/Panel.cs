using System;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Panel")]
  public class Panel : FamilyInstance
  {
    protected override Type ScriptVariableType => typeof(DB.FamilyInstance);
    public static explicit operator DB.FamilyInstance(Panel value) => value?.Value;
    public new DB.FamilyInstance Value => base.Value as DB.FamilyInstance;

    protected override bool SetValue(DB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(DB.Element element)
    {
      return element is DB.Panel ||
             element is DB.FamilyInstance instance && instance.Symbol.Family.IsCurtainPanelFamily;
    }

    public Panel() { }
    public Panel(DB.FamilyInstance value) : base(value) { }

    public override ElementType Type
    {
      get
      {
        if
        (
          Value is DB.Panel panel &&
          panel.Document.GetElement(panel.FindHostPanel()) is DB.HostObject host
        )
        {
          return ElementType.FromElementId(panel.Document, host.GetTypeId()) as ElementType;
        }
        else return base.Type;
      }
      set
      {
        if
        (
          Value is DB.Panel panel &&
          panel.Document.GetElement(panel.FindHostPanel()) is DB.HostObject host &&
          value?.Value is DB.HostObjAttributes hostType
        )
        {
          AssertValidDocument(value.Document, nameof(Type));
          InvalidateGraphics();

          host.ChangeTypeId(hostType.Id);
        }
        else base.Type = value;
      }
    }
  }
}
