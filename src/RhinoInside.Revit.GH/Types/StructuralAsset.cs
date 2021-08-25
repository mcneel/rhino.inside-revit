using System;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Name("Structural Asset")]
  public class StructuralAssetElement : Element
  {
    protected override Type ValueType => typeof(DB.PropertySetElement);
    public new DB.PropertySetElement Value => base.Value as DB.PropertySetElement;

    protected override bool SetValue(DB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static bool IsValidElement(DB.Element element)
    {
      if (element is DB.PropertySetElement pset)
      {
        try { return pset.GetStructuralAsset() is DB.StructuralAsset; }
        catch { }
      }

      return false;
    }

    public StructuralAssetElement() { }
    public StructuralAssetElement(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public StructuralAssetElement(DB.PropertySetElement asset) : base(asset) { }
  }
}
