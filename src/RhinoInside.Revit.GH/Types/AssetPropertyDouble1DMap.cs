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
using RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class AssetPropertyDouble1DMap : GH_Goo<DBX.AssetParameterDouble1DMap>
  {
    public override string TypeName => "Mappable Asset Property 1D";
    public override string TypeDescription
      => "Represents a double[1] property that accepts a texture map";
    public override bool IsValid => Value != null;
    public override sealed IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

    public AssetPropertyDouble1DMap() { }
    public AssetPropertyDouble1DMap(DBX.AssetParameterDouble1DMap prop): base(prop) { }

    public override bool CastFrom(object source)
    {
      switch (source)
      {
        case DBX.AssetParameterDouble1DMap prop:
          Value = prop;
          return true;

        case DBX.TextureData tdata:
          Value = new DBX.AssetParameterDouble1DMap(tdata);
          return true;

        case GH_Number number:
          Value = new DBX.AssetParameterDouble1DMap(number.Value);
          return true;
      }

      return base.CastFrom(source);
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(DBX.AssetParameterDouble1DMap)))
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

      return $"{Value.GetType().Name}";
    }
  }
}
