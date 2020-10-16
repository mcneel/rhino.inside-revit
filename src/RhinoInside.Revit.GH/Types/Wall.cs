using System;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Geometry.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class Wall : HostObject
  {
    public override string TypeDescription => "Represents a Revit wall element";
    protected override Type ScriptVariableType => typeof(DB.Wall);
    public static explicit operator DB.Wall(Wall value) => value?.Value;
    public new DB.Wall Value => base.Value as DB.Wall;

    public Wall() { }
    public Wall(DB.Wall host) : base(host) { }

    public override Plane Location
    {
      get
      {
        if (Value?.Location is DB.LocationCurve curveLocation)
        {
          var start = curveLocation.Curve.Evaluate(0.0, normalized: true).ToPoint3d();
          var end = curveLocation.Curve.Evaluate(1.0, normalized: true).ToPoint3d();
          var axis = end - start;
          var origin = start + (axis * 0.5);
          var perp = axis.PerpVector();
          return new Plane(origin, axis, perp);
        }

        return base.Location;
      }

      set
      {
        var joinTypes = new DB.JoinType[2] { DB.JoinType.None, DB.JoinType.None };

        if (Value is DB.Wall wall && wall.Location is DB.LocationCurve location)
        {
          for (int end = 0; end < 2; ++end)
          {
            if (DB.WallUtils.IsWallJoinAllowedAtEnd(wall, end))
            {
              joinTypes[end] = location.get_JoinType(end);
              DB.WallUtils.DisallowWallJoinAtEnd(wall, end);
            }
          }
        }

        base.Location = value;

        if (Value is DB.Wall newWall && newWall.Location is DB.LocationCurve newLocation)
        {
          for (int end = 0; end < 2; ++end)
          {
            if(joinTypes[end] != DB.JoinType.None)
            {
              DB.WallUtils.AllowWallJoinAtEnd(newWall, end);
              newLocation.set_JoinType(end, joinTypes[end]);
            }
          }
        }
      }
    }

    public override Curve Curve => Value?.Location is DB.LocationCurve curveLocation ?
      curveLocation.Curve.ToCurve() :
      default;
  }
}
