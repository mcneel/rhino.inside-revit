using Grasshopper.Kernel.Types;

using MAT = RhinoInside.Revit.GH.Components.Materials;

namespace RhinoInside.Revit.GH.Types
{
#if REVIT_2018
  public class TextureData : GH_Goo<MAT.TextureData>
  {
    public override bool IsValid => Value is object;

    public override string TypeName => IsValid ?
      Value.GetGHComponentInfo().Name :
      $"Texture Data";

    public override string TypeDescription => $"Represents a {TypeName}";

    public TextureData() : base() { }
    public TextureData(MAT.TextureData textureData) : base(textureData) { }

    public override IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

    public override bool CastFrom(object source)
    {
      switch (source)
      {
        case AssetPropertyDouble1DMap double1Map:
          if (double1Map.Value.HasTexture)
          {
            Value = double1Map.Value.TextureValue;
            return true;
          }
          break;

        case AssetPropertyDouble4DMap double4Map:
          if (double4Map.Value.HasTexture)
          {
            Value = double4Map.Value.TextureValue;
            return true;
          }
          break;

        case MAT.TextureData tdata:
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

      return base.CastTo(ref target);
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
