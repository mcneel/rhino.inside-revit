using System;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
#if REVIT_2019
  [Name("Structural Asset")]
  public class StructuralAsset : Element
  {
    protected override Type ScriptVariableType => typeof(DB.PropertySetElement);
    public static explicit operator DB.PropertySetElement(StructuralAsset value) =>
      value?.IsValid == true ? value.Value as DB.PropertySetElement : default;

    public StructuralAsset() { }
    public StructuralAsset(DB.PropertySetElement asset) : base(asset) { }
  }
#endif
}
