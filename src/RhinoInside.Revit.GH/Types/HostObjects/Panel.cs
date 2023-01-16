using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Panel")]
  public class Panel : FamilyInstance
  {
    static readonly ARDB.ElementId CurtainWallPanelsId = new ARDB.ElementId(ARDB.BuiltInCategory.OST_CurtainWallPanels);
    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      if (element is ARDB.Panel)
        return true;

      if (element is ARDB.FamilyInstance instance)
      {
        var symbol = instance.Symbol;
        if (symbol.Family.IsCurtainPanelFamily)
          return true;

        if (symbol.IsSimilarType(symbol.Document.GetDefaultFamilyTypeId(CurtainWallPanelsId)))
          return true;
      }

      return false;
    }

    public Panel() { }
    public Panel(ARDB.FamilyInstance value) : base(value) { }

    public override ElementType Type
    {
      get
      {
        if
        (
          Value is ARDB.Panel panel &&
          panel.Document.GetElement(panel.FindHostPanel()) is ARDB.HostObject host
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
          Value is ARDB.Panel panel &&
          panel.Document.GetElement(panel.FindHostPanel()) is ARDB.HostObject host &&
          value?.Value is ARDB.HostObjAttributes hostType
        )
        {
          AssertValidDocument(value, nameof(Type));
          InvalidateGraphics();

          host.ChangeTypeId(hostType.Id);
        }
        else base.Type = value;
      }
    }
  }

  [Kernel.Attributes.Name("Panel Type")]
  public class PanelType : FamilySymbol
  {
    static readonly ARDB.ElementId CurtainWallPanelsId = new ARDB.ElementId(ARDB.BuiltInCategory.OST_CurtainWallPanels);
    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static bool IsValidElement(ARDB.Element element)
    {
      if (element is ARDB.PanelType)
        return true;

      if (element is ARDB.FamilySymbol symbol)
      {
        if (symbol.Family.IsCurtainPanelFamily)
          return true;

        if (symbol.IsSimilarType(symbol.Document.GetDefaultFamilyTypeId(CurtainWallPanelsId)))
          return true;
      }

      return false;
    }

    public PanelType() { }
    public PanelType(ARDB.FamilySymbol value) : base(value) { }
  }
}
