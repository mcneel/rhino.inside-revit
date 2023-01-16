using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Structural Asset")]
  public class StructuralAssetElement : Element
  {
    protected override Type ValueType => typeof(ARDB.PropertySetElement);
    public new ARDB.PropertySetElement Value => base.Value as ARDB.PropertySetElement;

    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static bool IsValidElement(ARDB.Element element)
    {
      if (element is ARDB.PropertySetElement pset)
      {
        try { return pset.GetStructuralAsset() is ARDB.StructuralAsset; }
        catch { }
      }

      return false;
    }

    public StructuralAssetElement() { }
    public StructuralAssetElement(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public StructuralAssetElement(ARDB.PropertySetElement asset) : base(asset) { }
  }
}
