using System;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Name("Thermal Asset")]
  public class ThermalAssetElement : Element
  {
    protected override Type ScriptVariableType => typeof(DB.PropertySetElement);
    public new DB.PropertySetElement Value => base.Value as DB.PropertySetElement;

    protected override bool SetValue(DB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static bool IsValidElement(DB.Element element)
    {
      if (element is DB.PropertySetElement pset)
      {
        try { return pset.GetThermalAsset() is DB.ThermalAsset; }
        catch { }
      }

      return false;
    }

    public ThermalAssetElement() { }
    public ThermalAssetElement(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public ThermalAssetElement(DB.PropertySetElement asset) : base(asset) { }
  }
}
