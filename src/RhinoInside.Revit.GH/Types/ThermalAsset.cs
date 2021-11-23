using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Thermal Asset")]
  public class ThermalAssetElement : Element
  {
    protected override Type ValueType => typeof(ARDB.PropertySetElement);
    public new ARDB.PropertySetElement Value => base.Value as ARDB.PropertySetElement;

    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static bool IsValidElement(ARDB.Element element)
    {
      if (element is ARDB.PropertySetElement pset)
      {
        try { return pset.GetThermalAsset() is ARDB.ThermalAsset; }
        catch { }
      }

      return false;
    }

    public ThermalAssetElement() { }
    public ThermalAssetElement(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public ThermalAssetElement(ARDB.PropertySetElement asset) : base(asset) { }
  }
}
