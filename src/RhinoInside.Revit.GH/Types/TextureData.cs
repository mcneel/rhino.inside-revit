using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel.Types;

using MAT = RhinoInside.Revit.GH.Components.Element.Material;
using DB = Autodesk.Revit.DB;
using RhinoInside.Revit.GH.Components.Element.Material;

namespace RhinoInside.Revit.GH.Types
{
#if REVIT_2019
  public class TextureData : GH_Goo<MAT.TextureData>
  {
    public override bool IsValid => Value != null;

    public override string TypeName
    {
      get
      {
        if (IsValid)
          return Value.GetGHComponentInfo().Name;
        else
          return $"Invalid Asset";
      }
    }

    public override string TypeDescription => $"Represents a {TypeName}";

    public TextureData() : base() { }
    public TextureData(MAT.TextureData textureData) : base(textureData) { }

    public override IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

    public override bool CastFrom(object source)
    {
      if (source is MAT.TextureData tdata)
      {
        Value = tdata;
        return true;
      }

      return base.CastFrom(source);
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(MAT.TextureData)))
      {
        target = (Q) (object) Value;
        return true;
      }

      return base.CastTo<Q>(ref target);
    }

    public override string ToString()
    {
      return IsValid ?
             Value.ToString() :
             $"Invalid {TypeName}";
    }
  }
#endif
}
