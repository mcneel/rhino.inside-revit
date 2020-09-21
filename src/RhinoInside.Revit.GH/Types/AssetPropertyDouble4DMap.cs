using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel.Types;
using MAT = RhinoInside.Revit.GH.Components.Element.Material;
using DB = Autodesk.Revit.DB;
using Rhino.Geometry;
using UIFramework;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
#if REVIT_2019
  public class AssetPropertyDouble4DMap : GH_Goo<MAT.AssetPropertyDouble4DMap>
  {
    public override string TypeName => "Mappable Color";
    public override string TypeDescription
      => "Represents a double[4] property that accepts a texture map";
    public override bool IsValid => Value != null;
    public override sealed IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

    public AssetPropertyDouble4DMap() { }
    public AssetPropertyDouble4DMap(MAT.AssetPropertyDouble4DMap prop)
      : base(prop) { }

    public override bool CastFrom(object source)
    {
      switch (source)
      {
        case double dblValue:
          Value = new MAT.AssetPropertyDouble4DMap(dblValue);
          return true;

        case System.Drawing.Color colValue:
          Value = new MAT.AssetPropertyDouble4DMap(colValue);
          return true;

        case MAT.AssetPropertyDouble4DMap prop:
          Value = prop;
          return true;

        case MAT.TextureData tdata:
          Value = new MAT.AssetPropertyDouble4DMap(tdata);
          return true;

        case TextureData ttype:
          Value = new MAT.AssetPropertyDouble4DMap(ttype.Value);
          return true;

        case GH_Number number:
          Value = new MAT.AssetPropertyDouble4DMap(number.Value);
          return true;

        case GH_Colour color:
          Value = new MAT.AssetPropertyDouble4DMap(color.Value);
          return true;
      }

      return base.CastFrom(source);
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(MAT.AssetPropertyDouble4DMap)))
      {
        target = (Q) (object) Value;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(MAT.TextureData)))
      {
        target = (Q) (object) Value;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Number)))
      {
        target = (Q) (object) new GH_Number(Value.Average);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Colour)))
      {
        target = (Q) (object) new GH_Colour(Value.ValueAsColor);
        return true;
      }

      return base.CastTo<Q>(ref target);
    }

    public override string ToString()
    {
      if (!IsValid)
        return $"Invalid {TypeName}";

      if (Value.HasTexture)
        return $"{Value.TextureValue}";
      else
        return $"{new GH_Colour(Value.ValueAsColor)}";
    }
  }
#endif
}
