using System;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Name("Appearance Asset")]
  public class AppearanceAsset : Element
  {
    protected override Type ScriptVariableType => typeof(DB.AppearanceAssetElement);
    public static explicit operator DB.AppearanceAssetElement(AppearanceAsset value) =>
      value?.IsValid == true ? value.Value as DB.AppearanceAssetElement : default;

    public AppearanceAsset() { }
    public AppearanceAsset(DB.AppearanceAssetElement asset) : base(asset) { }
  }
}
