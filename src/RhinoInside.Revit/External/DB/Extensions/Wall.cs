using System;
using System.Collections.Generic;
using System.Linq;

using Rhino.Geometry;
using Autodesk.Revit.DB;
using RhinoInside.Revit;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class WallExtension
  {
    /// <summary>
    /// Returns orientation vector of the wall corrected for wall flip
    /// </summary>
    /// <returns>Orientation vector</returns>
    public static Vector3d GetOrientationVector(this Wall wall)
    {
      var wallOrientationVector = new Vector3d(wall.Orientation.X, wall.Orientation.Y, wall.Orientation.Z);
      if (wall.Flipped)
        wallOrientationVector.Reverse();
      return wallOrientationVector;
    }

    /// <summary>
    /// Return total width of the wall
    /// </summary>
    /// <returns>Total width</returns>
    public static double GetWidth(this Wall wall)
    {
      // for some reason the base Width unit for Curtain walls is different
      return wall.WallType.Kind == WallKind.Curtain ? wall.Width * 12 : wall.Width;

    }

    /// <summary>
    /// Return LocationCurve of the wall
    /// </summary>
    /// <returns>Wall Location Curve</returns>
    public static LocationCurve GetLocationCurve(this Wall wall) => wall.Location as LocationCurve;

    /// <summary>
    /// Return center curve of the wall
    /// </summary>
    /// <param name="wall"></param>
    /// <returns></returns>
    public static Rhino.Geometry.Curve GetCenterCurve(this Wall wall)
    {
      // TODO: stacked walls center line is not correct
      return wall.GetLocationCurve().Curve.ToRhino();
    }

    /// <summary>
    /// Return bounding geometry of the wall
    /// Bounding geometry is the geometry wrapping Stacked or Curtain walls (different from Bounding Box).
    /// For Basic Walls the bounding geometry is identical to the default wall geometry
    /// </summary>
    /// <returns>Bounding geometry of a wall</returns>
    public static Brep ComputeWallBoundingGeometry(this Wall wall)
    {
      // TODO: brep creation might be crude and could use performance improvements
      // TODO: update for 2021 and slanted walls

      // extract global properties
      // e.g. base height, thickness, ...
      var height = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble();
      var thickness = wall.GetWidth();
      // construct a base offset plane that is used later to offset base curves
      var offsetPlane = Rhino.Geometry.Plane.WorldXY;
      var baseTransform = new Vector3d(0, 0, 0);
      baseTransform.Z = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).AsDouble();


      // get the base curve of wall (center curve), and wall thickness
      // this will be used to create a bottom-profile of the wall
      var baseCurve = Convert.ToRhino(((LocationCurve) wall.Location).Curve);
      // transform to where the wall base is
      baseCurve.Translate(baseTransform);

      // create the base surface (loft from start and end curves, offset from wall center curve)
      var startCurve = baseCurve.Offset(offsetPlane, thickness / 2.0, 0.1, CurveOffsetCornerStyle.None)[0];
      var endCurve = baseCurve.Offset(offsetPlane, thickness / -2.0, 0.1, CurveOffsetCornerStyle.None)[0];

      var bloft = Brep.CreateFromLoft(
          curves: new List<Rhino.Geometry.Curve>() { startCurve, endCurve },
          start: Point3d.Unset,
          end: Point3d.Unset,
          loftType: LoftType.Normal,
          closed: false
          )[0]; // grab the first loft
      // grab the face of the base surface
      var bface = bloft.Faces[0];

      // calculate extrusion height
      var fromPoint = startCurve.PointAtStart;
      var toPoint = new Point3d(fromPoint);
      toPoint.Z += height;

      // and extrude the surface, and return
      return bface.CreateExtrusion(
        pathCurve: new Rhino.Geometry.Line(fromPoint, toPoint).ToNurbsCurve(),
        cap: true
        );
    }
  }
}
