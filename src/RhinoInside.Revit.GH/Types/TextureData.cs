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
  public class TextureData<T> :GH_Goo<T> where T: MAT.TextureData, new()
  {
    public override bool IsValid => Value != null;
    public override string TypeName
    {
      get
      {
        if (IsValid)
          return Value.ToString();
        else
          return $"Revit Texture (Unset)";
      }
    }
    public override string TypeDescription => $"Represents a {TypeName}";

    public TextureData() : base () { }
    public TextureData(T textureData) : base (textureData) { }

    public override IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

    public override bool CastFrom(object source)
    {
      if (source is T tdata)
      {
        Value = tdata;
        return true;
      }

      return base.CastFrom(source);
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(T)))
      {
        target = (Q) (object) Value;
        return true;
      }

      return base.CastTo<Q>(ref target);
    }

    public static implicit operator TextureData<T>(TextureData<MAT.TextureData> target)
    {
      if (target.Value is T tValue)
        return new TextureData<T>(tValue);
      throw new System.InvalidCastException();
    }

    public override string ToString()
    {
      return IsValid ?
             TypeName :
             $"Invalid {TypeName}";
    }
  }
}
