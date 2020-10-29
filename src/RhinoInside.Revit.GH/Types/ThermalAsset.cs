using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
#if REVIT_2019
  [Name("Thermal Asset")]
  public class ThermalAsset : Element
  {
    protected override Type ScriptVariableType => typeof(DB.PropertySetElement);
    public static explicit operator DB.PropertySetElement(ThermalAsset value) =>
      value?.IsValid == true ? value.Value as DB.PropertySetElement : default;

    public ThermalAsset() { }
    public ThermalAsset(DB.PropertySetElement asset) : base(asset) { }
  }
#endif
}
