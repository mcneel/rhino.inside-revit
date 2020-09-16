using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel.Types;

using DBX = RhinoInside.Revit.External.DB;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class TextureData<T> :GH_Goo<T> where T: DBX.TextureData, new()
  {
    public override bool IsValid => Value != null;
    public override string TypeName
    {
      get
      {
        if (IsValid)
          return $"Revit Texture ({Value.Schema})";
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
      if (typeof(Q).IsAssignableFrom(typeof(DBX.TextureData)))
      {
        target = (Q) (object) Value;
        return true;
      }

      return base.CastTo<Q>(ref target);
    }

    public override string ToString()
    {
      return IsValid ?
             TypeName :
             $"Invalid {TypeName}";
    }
  }
}
