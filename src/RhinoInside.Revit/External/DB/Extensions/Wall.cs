using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class WallExtension
  {
    /// <summary>
    /// Returns orientation vector of the wall corrected for wall flip
    /// </summary>
    /// <returns>Orientation vector</returns>
    public static Rhino.Geometry.Vector3d GetOrientationVector(this Wall wall)
    {
      var wallOrientationVector = wall.Flipped ?
        -wall.Orientation.ToVector3d() :
         wall.Orientation.ToVector3d();

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
      return wall.GetLocationCurve().Curve.ToCurve();
    }

    /// <summary>
    /// Return bounding geometry of the wall
    /// Bounding geometry is the geometry wrapping Stacked or Curtain walls (different from Bounding Box).
    /// For Basic Walls the bounding geometry is identical to the default wall geometry
    /// </summary>
    /// <returns>Bounding geometry of a wall</returns>
    public static Rhino.Geometry.Brep ComputeWallBoundingGeometry(this Wall wall)
    {
      // TODO: brep creation might be crude and could use performance improvements

      // extract global properties
      // e.g. base height, thickness, ...
      var height = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble() * Revit.ModelUnits;
      var thickness = wall.GetWidth() * Revit.ModelUnits;
      // construct a base offset plane that is used later to offset base curves
      var offsetPlane = Rhino.Geometry.Plane.WorldXY;
      var baseElevation = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).AsDouble();

      // calculate slant
      double topOffset = double.NaN;
#if REVIT_2021
      // if wall slant is supported grab the slant angle
      var slantParam = wall.get_Parameter(BuiltInParameter.WALL_SINGLE_SLANT_ANGLE_FROM_VERTICAL);
      if (slantParam is Parameter)
      {
        // and calculate the cuvre offset at the top based on the curve slant angle
        //     O = top offset distance
        // ---------
        //  \      |
        //   \  S = slant angle
        //    \    |
        //     \   | H = wall height
        //      \  |
        //       \ |
        //        \|
        var slantAngle = slantParam.AsDouble();
        if (slantAngle > 0)
          topOffset = height * (Math.Sin(slantAngle) / Math.Abs(Math.Cos(slantAngle)));
      }
#endif


      // get the base curve of wall (center curve), and wall thickness
      // this will be used to create a bottom-profile of the wall
      var baseCurve = ((LocationCurve) wall.Location).Curve.ToCurve();
      // transform to where the wall base is
      baseCurve.Translate(0, 0, baseElevation);

      // create the base curves on boths sides
      var side1BottomCurve = baseCurve.Offset(offsetPlane, thickness / 2.0, 0.1, Rhino.Geometry.CurveOffsetCornerStyle.None)[0];
      var side2BottomCurve = baseCurve.Offset(offsetPlane, thickness / -2.0, 0.1, Rhino.Geometry.CurveOffsetCornerStyle.None)[0];

      // create top curves, by moving a duplicate of base curves to top
      var fromPoint = side1BottomCurve.PointAtStart;
      var side1TopCurve = side1BottomCurve.DuplicateCurve();
      side1TopCurve.Translate(0, 0, fromPoint.Z + height);
      var side2TopCurve = side2BottomCurve.DuplicateCurve();
      side2TopCurve.Translate(0, 0, fromPoint.Z + height);

      // offset the top curves to get the slanted wall top curves, based on the previously calculated offset distance
      if (topOffset > 0)
      {
        side1TopCurve = side1TopCurve.Offset(offsetPlane, topOffset, 0.1, CurveOffsetCornerStyle.None)[0];
        side2TopCurve = side2TopCurve.Offset(offsetPlane, topOffset, 0.1, CurveOffsetCornerStyle.None)[0];
      }

      // build a list of curve-pairs for the 6 sides
      var sideCurvePairs = new List<Tuple<Rhino.Geometry.Curve, Rhino.Geometry.Curve>>()
      {
        // side 1
        new Tuple<Rhino.Geometry.Curve, Rhino.Geometry.Curve>(side1BottomCurve, side1TopCurve),
        // side 2
        new Tuple<Rhino.Geometry.Curve, Rhino.Geometry.Curve>(side2BottomCurve, side2TopCurve),
        // bottom
        new Tuple<Rhino.Geometry.Curve, Rhino.Geometry.Curve>(side1BottomCurve, side2BottomCurve),
        // start side
        new Tuple<Rhino.Geometry.Curve, Rhino.Geometry.Curve>(
          new Rhino.Geometry.Line(side1BottomCurve.PointAtStart, side1TopCurve.PointAtStart).ToNurbsCurve(),
          new Rhino.Geometry.Line(side2BottomCurve.PointAtStart, side2TopCurve.PointAtStart).ToNurbsCurve()
        ),
        // top
        new Tuple<Rhino.Geometry.Curve, Rhino.Geometry.Curve>(side1TopCurve, side2TopCurve),
        // end side
        new Tuple<Rhino.Geometry.Curve, Rhino.Geometry.Curve>(
          new Rhino.Geometry.Line(side1TopCurve.PointAtEnd, side1BottomCurve.PointAtEnd).ToNurbsCurve(),
          new Rhino.Geometry.Line(side2TopCurve.PointAtEnd, side2BottomCurve.PointAtEnd).ToNurbsCurve()
        )
      };

      // build breps for each side and add to list
      var finalBreps = new List<Brep>();
      foreach (var curvePair in sideCurvePairs)
      {
        var loft = Brep.CreateFromLoft(
            curves: new List<Rhino.Geometry.Curve>() { curvePair.Item1, curvePair.Item2 },
            start: Point3d.Unset,
            end: Point3d.Unset,
            loftType: LoftType.Normal,
            closed: false
            ).First(); // grab the first loft
        finalBreps.Add(loft);
      }
      // join all the breps into one
      return Brep.JoinBreps(finalBreps, 0.1).First();
    }
  }
}
