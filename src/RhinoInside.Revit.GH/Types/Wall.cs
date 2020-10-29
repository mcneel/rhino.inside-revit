using System;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Geometry.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Wall")]
  public class Wall : HostObject
  {
    protected override Type ScriptVariableType => typeof(DB.Wall);
    public static explicit operator DB.Wall(Wall value) => value?.Value;
    public new DB.Wall Value => base.Value as DB.Wall;

    public Wall() { }
    public Wall(DB.Wall host) : base(host) { }

    #region Location
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
    }

    public override Vector3d FacingOrientation => Value?.Flipped == true ? -Location.YAxis : Location.YAxis;

    public static bool IsValidCurve(Curve curve, out string log)
    {
      if (!curve.IsValidWithLog(out log)) return false;

#if REVIT_2020
      if
      (
        !(curve.IsLinear(Revit.VertexTolerance * Revit.ModelUnits) || curve.IsArc(Revit.VertexTolerance * Revit.ModelUnits) || curve.IsEllipse(Revit.VertexTolerance * Revit.ModelUnits)) ||
        !curve.TryGetPlane(out var axisPlane, Revit.VertexTolerance * Revit.ModelUnits) ||
        axisPlane.ZAxis.IsParallelTo(Vector3d.ZAxis) == 0
      )
      {
        log = "Curve should be a horizontal line, arc or ellipse curve.";
        return false;
      }
#else
      if
      (
        !(curve.IsLinear(Revit.VertexTolerance * Revit.ModelUnits) || curve.IsArc(Revit.VertexTolerance * Revit.ModelUnits)) ||
        !curve.TryGetPlane(out var axisPlane, Revit.VertexTolerance * Revit.ModelUnits) ||
        axisPlane.ZAxis.IsParallelTo(Vector3d.ZAxis) == 0
      )
      {
        log = "Curve should be a horizontal line or arc curve.";
        return false;
      }
#endif

      return true;
    }

    public override Curve Curve
    {
      set
      {
        if (value is object && Value is DB.Wall wall && wall.Location is DB.LocationCurve)
        {
          if (!IsValidCurve(value, out var log))
            throw new ArgumentException(nameof(Curve), log);

          switch (Curve)
          {
            case LineCurve _:
              if (value.TryGetLine(out var valueLine, Revit.VertexTolerance * Revit.ModelUnits))
                base.Curve = new LineCurve(valueLine);
              else
                throw new ArgumentException(nameof(Curve), "Curve should be a line like curve.");
              break;
            case ArcCurve _:
              if (value.TryGetArc(out var valueArc, Revit.VertexTolerance * Revit.ModelUnits))
                base.Curve = new ArcCurve(valueArc);
              else
                throw new ArgumentException(nameof(Curve), "Curve should be an arc like curve.");
              break;
            case NurbsCurve _:
              if (value.TryGetEllipse(out var _, Revit.VertexTolerance * Revit.ModelUnits))
                base.Curve = value;
              else
                throw new ArgumentException(nameof(Curve), "Curve should be an ellipse like curve.");
              break;
          }
        }
      }
    }
    #endregion

    #region Joins
    public override bool? IsJoinAllowedAtStart
    {
      get =>  Value is DB.Wall wall ?
        DB.WallUtils.IsWallJoinAllowedAtEnd(wall, 0) :
        default;

      set
      {
        if (value is object && Value is DB.Wall wall && value != IsJoinAllowedAtEnd)
        {
          if (value == true)
            DB.WallUtils.AllowWallJoinAtEnd(wall, 0);
          else
            DB.WallUtils.DisallowWallJoinAtEnd(wall, 0);
        }
      }
    }

    public override bool? IsJoinAllowedAtEnd
    {
      get => Value is DB.Wall wall ?
        DB.WallUtils.IsWallJoinAllowedAtEnd(wall, 1) :
        default;

      set
      {
        if (value is object && Value is DB.Wall wall && value != IsJoinAllowedAtEnd)
        {
          if (value == true)
            DB.WallUtils.AllowWallJoinAtEnd(wall, 1);
          else
            DB.WallUtils.DisallowWallJoinAtEnd(wall, 1);
        }
      }
    }
    #endregion
  }
}
