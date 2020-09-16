using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel.Types;
using DBX = RhinoInside.Revit.External.DB;
using DB = Autodesk.Revit.DB;
using Rhino.Geometry;
using UIFramework;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class AssetPropertyDouble4DMap : GH_Goo<DBX.AssetParameterDouble4DMap>
  {
    public override string TypeName => "Mappable Asset Property 4D";
    public override string TypeDescription
      => "Represents a double[4] property that accepts a texture map";
    public override bool IsValid => Value != null;
    public override sealed IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

    public AssetPropertyDouble4DMap() { }
    public AssetPropertyDouble4DMap(DBX.AssetParameterDouble4DMap prop)
      : base(prop) { }

    public override bool CastFrom(object source)
    {
      switch (source)
      {
        case DBX.AssetParameterDouble4DMap prop:
          Value = prop;
          return true;

        case DBX.TextureData tdata:
          Value = new DBX.AssetParameterDouble4DMap(tdata);
          return true;

        case GH_Number number:
          Value = new DBX.AssetParameterDouble4DMap(number.Value);
          return true;

        case GH_Colour color:
          Value = new DBX.AssetParameterDouble4DMap(color.Value);
          return true;
      }

      return base.CastFrom(source);
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(DBX.AssetParameterDouble4DMap)))
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

      return $"{Value.GetType().Name}";
    }
  }
}
