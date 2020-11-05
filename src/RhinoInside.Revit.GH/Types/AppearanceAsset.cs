using System;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Name("Appearance Asset")]
  public class AppearanceAssetElement : Element
  {
    protected override Type ScriptVariableType => typeof(DB.AppearanceAssetElement);
    public new DB.AppearanceAssetElement Value => base.Value as DB.AppearanceAssetElement;

    public AppearanceAssetElement() { }
    public AppearanceAssetElement(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public AppearanceAssetElement(DB.AppearanceAssetElement asset) : base(asset) { }
  }
}
