using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class AppearanceAsset : Element
  {
    public override string TypeName => "Revit Appearance Asset";
    public override string TypeDescription => "Represents a Revit Appearance Asset";
    protected override Type ScriptVariableType => typeof(DB.AppearanceAssetElement);
    public static explicit operator DB.AppearanceAssetElement(AppearanceAsset value) =>
      value?.IsValid == true ? value.APIElement as DB.AppearanceAssetElement : default;

    public AppearanceAsset() { }
    public AppearanceAsset(DB.AppearanceAssetElement asset) : base(asset) { }
  }
}
