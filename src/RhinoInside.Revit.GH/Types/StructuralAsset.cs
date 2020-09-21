using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
#if REVIT_2019
  public class StructuralAsset : Element
  {
    public override string TypeName => "Revit Physical Asset";
    public override string TypeDescription => "Represents a Revit Physical Asset";
    protected override Type ScriptVariableType => typeof(DB.PropertySetElement);
    public static explicit operator DB.PropertySetElement(StructuralAsset value) =>
      value?.IsValid == true ? value.APIElement as DB.PropertySetElement : default;

    public StructuralAsset() { }
    public StructuralAsset(DB.PropertySetElement asset) : base(asset) { }
  }
#endif
}
