using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
#if REVIT_2019
  public class ThermalAsset : Element
  {
    public override string TypeName => "Revit Thermal Asset";
    public override string TypeDescription => "Represents a Revit Thermal Asset";
    protected override Type ScriptVariableType => typeof(DB.PropertySetElement);
    public static explicit operator DB.PropertySetElement(ThermalAsset value) =>
      value?.IsValid == true ? value.Value as DB.PropertySetElement : default;

    public ThermalAsset() { }
    public ThermalAsset(DB.PropertySetElement asset) : base(asset) { }
  }
#endif
}
