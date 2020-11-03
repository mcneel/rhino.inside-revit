
using Grasshopper.Kernel.Types;
using MAT = RhinoInside.Revit.GH.Components.Material;

namespace RhinoInside.Revit.GH.Types
{
#if REVIT_2019
  public class AssetPropertyDouble1DMap : GH_Goo<MAT.AssetPropertyDouble1DMap>
  {
    public override string TypeName => "Mappable Double";
    public override string TypeDescription
      => "Represents a double[1] property that accepts a texture map";
    public override bool IsValid => Value != null;
    public override sealed IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

    public AssetPropertyDouble1DMap() { }
    public AssetPropertyDouble1DMap(MAT.AssetPropertyDouble1DMap prop) : base(prop) { }

    public override bool CastFrom(object source)
    {
      switch (source)
      {
        case double dblValue:
          Value = new MAT.AssetPropertyDouble1DMap(dblValue);
          return true;

        case MAT.AssetPropertyDouble1DMap prop:
          Value = prop;
          return true;

        case MAT.TextureData tdata:
          Value = new MAT.AssetPropertyDouble1DMap(tdata);
          return true;

        case TextureData ttype:
          Value = new MAT.AssetPropertyDouble1DMap(ttype.Value);
          return true;

        case GH_Number number:
          Value = new MAT.AssetPropertyDouble1DMap(number.Value);
          return true;
      }

      return base.CastFrom(source);
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(MAT.AssetPropertyDouble1DMap)))
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
        target = (Q) (object) new GH_Number(Value.Value);
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
        return $"{Value.Value}";
    }
  }
#endif
}
