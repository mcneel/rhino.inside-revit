
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using MAT = RhinoInside.Revit.GH.Components.Materials;

namespace RhinoInside.Revit.GH.Types
{
#if REVIT_2018
  public class AssetPropertyDouble4DMap : GH_Goo<MAT.AssetPropertyDouble4DMap>
  {
    public override string TypeName => "Mappable Color";
    public override string TypeDescription =>
      "Represents a double[4] property that accepts a texture map";
    public override bool IsValid => Value != null;
    public sealed override IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

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

        case Rhino.Display.ColorRGBA rgbaValue:
          Value = new MAT.AssetPropertyDouble4DMap(rgbaValue);
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

        case GH_ColorRGBA colorRGBA:
          Value = new MAT.AssetPropertyDouble4DMap(colorRGBA.Value);
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
        target = (Q) (object) Value.TextureValue;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Number)))
      {
        target = (Q) (object) new GH_Number(Value.Average);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_ColorRGBA)))
      {
        target = (Q) (object) new GH_ColorRGBA(Value.ToColorRGBA());
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Colour)))
      {
        target = (Q) (object) new GH_Colour((System.Drawing.Color) Value.ToColorRGBA());
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
        return GH_Format.FormatColour((System.Drawing.Color) Value.ToColorRGBA());
    }
  }
#endif
}
