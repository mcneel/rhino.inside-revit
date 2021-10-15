using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32.SafeHandles;

namespace Rhino.Geometry
{
  static class NaN
  {
    public static readonly double Value = double.NaN;
    public static readonly Interval Interval = new Interval(Value, Value);
    public static readonly Point3d Point3d = new Point3d(Value, Value, Value);
    public static readonly Vector3d Vector3d = new Vector3d(Value, Value, Value);
    public static readonly Plane Plane = new Plane(Point3d, Vector3d, Vector3d);
    public static readonly BoundingBox BoundingBox = new BoundingBox(Point3d, Point3d);
  }

  static class Vector3dExtension
  {
    /// <summary>
    /// Arbitrary Axis Algorithm
    /// <para>Given a vector to be used as the Z axis of a coordinate system, this algorithm generates a corresponding X axis for the coordinate system.</para>
    /// <para>The Y axis follows by application of the right-hand rule.</para>
    /// </summary>
    /// <param name="value"></param>
    /// <param name="tolerance"></param>
    /// <returns>X axis of the corresponding coordinate system</returns>
    public static Vector3d PerpVector(this Vector3d value, double tolerance = 1e-9)
    {
      var length = value.Length;
      if (length < tolerance)
        return Vector3d.Zero;

      var normal = value / length;

      if (Vector3d.Zero.EpsilonEquals(new Vector3d(normal.X, normal.Y, 0.0), tolerance))
        return new Vector3d(value.Z, 0.0, -value.X);
      else
        return new Vector3d(-value.Y, value.X, 0.0);
    }
  }

  static class BoundingBoxExtension
  {
    /// <summary>
    /// Aligned bounding box solver. Gets the world axis aligned bounding box for the transformed <paramref name="value"/>.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="xform">Transformation to apply to <paramref name="value"/> prior to the BoundingBox computation. The <paramref name="value"/> itself is not modified</param>
    /// <returns>The accurate bounding box of the transformed geometry in world coordinates or <see cref="BoundingBox.Unset"/> if not bounding box could be found</returns>
    public static BoundingBox GetBoundingBox(this BoundingBox value, Transform xform)
    {
      if (!value.IsValid)
        return NaN.BoundingBox;

      // BoundingBox constructor already checks for Identity xform
      //if (xform.IsIdentity)
      //  return value;

      return new BoundingBox(value.GetCorners(), xform);
    }
  }

  static class BoxExtension
  {
    /// <summary>
    /// Aligned bounding box solver. Gets the world axis aligned bounding box for the transformed <paramref name="value"/>.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="xform">Transformation to apply to <paramref name="value"/> prior to the BoundingBox computation. The <paramref name="value"/> itself is not modified</param>
    /// <returns>The accurate bounding box of the transformed geometry in world coordinates or <see cref="BoundingBox.Unset"/> if not bounding box could be found</returns>
    public static BoundingBox GetBoundingBox(this Box value, Transform xform)
    {
      if (!value.IsValid)
        return NaN.BoundingBox;

      // BoundingBox constructor already checks for Identity xform
      //if (xform.IsIdentity)
      //  return value.BoundingBox;

      return new BoundingBox(value.GetCorners(), xform);
    }
  }

  static class ExtrusionExtension
  {
    public static bool TryGetExtrusion(this Surface surface, out Extrusion extrusion)
    {
      extrusion = null;
      var nurbsSurface = surface as NurbsSurface ?? surface.ToNurbsSurface();

      for (int direction = 1; direction >= 0; --direction)
      {
        var oposite = direction == 0 ? 1 : 0;

        if (surface.IsClosed(direction))
          continue;

        var domain = nurbsSurface.Domain(direction);
        var iso0 = nurbsSurface.IsoCurve(oposite, domain.Min);
        var iso1 = nurbsSurface.IsoCurve(oposite, domain.Max);

        if (iso0.TryGetPlane(out var plane0) && iso1.TryGetPlane(out var plane1))
        {
          if (plane0.Normal.IsParallelTo(plane1.Normal, RhinoMath.DefaultAngleTolerance / 100.0) == 1)
          {
            var rowCount = direction == 0 ? nurbsSurface.Points.CountU : nurbsSurface.Points.CountV;
            var columnCount = direction == 0 ? nurbsSurface.Points.CountV : nurbsSurface.Points.CountU;
            for (int c = 0; c < columnCount; ++c)
            {
              var point = direction == 0 ? nurbsSurface.Points.GetControlPoint(0, c) : nurbsSurface.Points.GetControlPoint(c, 0);
              for (int r = 1; r < rowCount; ++r)
              {
                var pointR = direction == 0 ? nurbsSurface.Points.GetControlPoint(r, c) : nurbsSurface.Points.GetControlPoint(c, r);
                var projectedPointR = plane0.ClosestPoint(pointR.Location);
                if (projectedPointR.DistanceToSquared(point.Location) > RhinoMath.SqrtEpsilon)
                  return false;

                if (Math.Abs(pointR.Weight - point.Weight) > RhinoMath.ZeroTolerance)
                  return false;
              }
            }

            // Extrusion.Create does not work well when 'iso0' is a line-like curve,
            // plane used to extrude is "arbitrary" in this case
            //extrusion = Extrusion.Create(iso0, zAxis.Length, false);

            var axis = new Line(iso0.PointAtStart, iso1.PointAtStart);
            var zAxis = iso1.PointAtStart - iso0.PointAtStart;
            var xAxis = (iso0.IsClosed ? iso0.PointAt(iso0.Domain.Mid) : iso0.PointAtEnd) - iso0.PointAtStart;
            var yAxis = Vector3d.CrossProduct(zAxis, xAxis);

            extrusion = new Extrusion();
            if (!iso0.Transform(Transform.PlaneToPlane(new Plane(iso0.PointAtStart, xAxis, yAxis), Plane.WorldXY)))
              return false;

            return extrusion.SetPathAndUp(axis.From, axis.To, yAxis) && extrusion.SetOuterProfile(iso0, false);
          }
        }
      }

      return false;
    }

    public static bool TryGetExtrusion(this BrepFace face, out Extrusion extrusion)
    {
      if (face.UnderlyingSurface().TryGetExtrusion(out extrusion))
      {
        if (face.OrientationIsReversed)
        {
          var profile = extrusion.Profile3d(new ComponentIndex(ComponentIndexType.ExtrusionBottomProfile, 0));
          profile.Reverse();

          if (!extrusion.GetProfileTransformation(0.0).TryGetInverse(out var WCStoECS))
            return false;

          if (!profile.Transform(WCStoECS))
            return false;

          return extrusion.SetOuterProfile(profile, false);
        }

        return true;
      }

      extrusion = null;
      return false;
    }

    struct PlanarBrepFace
    {
      public PlanarBrepFace(BrepFace f)
      {
        Face = f;
        if (!Face.TryGetPlane(out Plane, RhinoMath.ZeroTolerance))
          Plane = Plane.Unset;

        loop = null;
        area = double.NaN;
        centroid = NaN.Point3d;
      }

      public readonly BrepFace Face;
      public readonly Plane Plane;
      NurbsCurve loop;
      Point3d centroid;
      double area;

      public NurbsCurve Loop
      {
        get { if (Plane.IsValid && loop is null) loop = Curve.ProjectToPlane(Face.OuterLoop.To3dCurve(), Plane).ToNurbsCurve(); return loop; }
      }
      public Point3d Centroid
      {
        get { if (!centroid.IsValid && Loop is object) using (var mp = AreaMassProperties.Compute(Loop, RhinoMath.ZeroTolerance)) if (mp is object) { area = mp.Area; centroid = mp.Centroid; } return centroid; }
      }
      public double LoopArea
      {
        get { if (double.IsNaN(area) && Loop is object) using (var mp = AreaMassProperties.Compute(Loop, RhinoMath.ZeroTolerance)) if (mp is object) { area = mp.Area; centroid = mp.Centroid; } return area; }
      }

      public bool ProjectionDegenartesToCurve(Surface surface)
      {
        // This function basically tests if 'surface' projected to 'plane' degenerate to a curve
        // So it can be used with any kind of surface even BrepFace, trims doesn't matter.
        // But if called with a BrepFace using UnderlyingSurface() may avoid one extra surface conversion
        var nurbsSurface = surface as NurbsSurface ?? surface.ToNurbsSurface();

        var domainU = nurbsSurface.Domain(0);
        var domainV = nurbsSurface.Domain(1);
        var isoU = nurbsSurface.IsoCurve(1, domainU.Min);
        var isoV = nurbsSurface.IsoCurve(0, domainV.Min);

        // To avoid problems with lines we test for perpendicularity instead planar parallelism
        // To avoid problems with closed curves we use the mid point instead of PointAtEnd
        // Self intersected curves are not allowed here, isoU and isoV are edges of a face,
        // so PointAt(Domain.Mid) will not be equal to PointAtStart
        int row = -1;
        if (isoU.IsPlanar() && (isoU.PointAtStart - isoU.PointAt(domainU.Mid)).IsPerpendicularTo(Plane.Normal))
          row = 0;
        else if (isoV.IsPlanar() && (isoV.PointAtStart - isoV.PointAt(domainV.Mid)).IsPerpendicularTo(Plane.Normal))
          row = 1;

        // No Edge parallel to plane
        if (row < 0)
          return false;

        // Test if projection of all rows of control points projected to 'plane' are coincident.
        // This means 'surface' degenerate to a curve if projected to 'plane', so an "extrusion".
        var rowCount = row == 0 ? nurbsSurface.Points.CountU : nurbsSurface.Points.CountV;
        var columnCount = row == 0 ? nurbsSurface.Points.CountV : nurbsSurface.Points.CountU;
        for (int c = 0; c < columnCount; ++c)
        {
          var point = row == 0 ? nurbsSurface.Points.GetControlPoint(0, c) : nurbsSurface.Points.GetControlPoint(c, 0);
          var projectedPoint = Plane.ClosestPoint(point.Location);
          for (int r = 1; r < rowCount; ++r)
          {
            var pointR = row == 0 ? nurbsSurface.Points.GetControlPoint(r, c) : nurbsSurface.Points.GetControlPoint(c, r);
            var projectedPointR = Plane.ClosestPoint(pointR.Location);

            if (projectedPointR.DistanceToSquared(projectedPoint) > RhinoMath.SqrtEpsilon)
              return false;

            if (Math.Abs(pointR.Weight - point.Weight) > RhinoMath.ZeroTolerance)
              return false;
          }
        }

        return true;
      }
    }

    public static bool TryGetExtrusion(this Brep brep, out Extrusion extrusion)
    {
      if (brep.IsSurface)
        return brep.Faces[0].TryGetExtrusion(out extrusion);

      extrusion = null;
      if (brep.Faces.Count < 3)
        return false;

      // Requiere manifold breps
      if (brep.SolidOrientation == BrepSolidOrientation.None || brep.SolidOrientation == BrepSolidOrientation.Unknown)
        return false;

      // If brep has more that 3 faces we should check if there are faces with interior loops
      if (brep.Faces.Count > 3 && brep.Faces.Where(face => face.Loops.Count != 1 && !face.IsPlanar(RhinoMath.ZeroTolerance)).Any())
        return false;

      var candidateFaces = new List<int[]>();

      // Array with just planar faces sorted by its area to search for similar faces
      var planarFaces = brep.Faces.
                        Select(face => new PlanarBrepFace(face)).
                        Where(face => face.Plane.IsValid).
                        OrderByDescending(face => face.LoopArea).
                        ToArray();

      // A capped Extrusion converted to Brep has wall surfaces in face[0] to face[N-3], caps are face[N-2] and face[N-1]
      // I iterate in reverse order to be optimisitc, maybe brep comes from an Extrusion.ToBrep() call
      for (int f = planarFaces.Length - 1; f > 0; --f)
      {
        var planeF = planarFaces[f].Plane;
        var loopF = planarFaces[f].Loop;
        var centroidF = planarFaces[f].Centroid;

        // Check if they have same area.
        for (int g = f - 1; g >= 0 && RhinoMath.EpsilonEquals(planarFaces[f].LoopArea, planarFaces[g].LoopArea, RhinoMath.SqrtEpsilon); --g)
        {
          // Planes should be parallel or anti-parallel
          if (planeF.Normal.IsParallelTo(planarFaces[g].Plane.Normal, RhinoMath.DefaultAngleTolerance / 100.0) == 0)
            continue;

          // Here f, and g are perfect candidates to test adjacent faces for perpendicularity to them,
          // but we may try to quick reject some candidates if it's obvious that doesn't match

          // A "perfect" curve overlap match may be a test but is too much in this ocasion

          // We expect same NURBS structure, so point count should match
          if (loopF.Points.Count != planarFaces[g].Loop.Points.Count)
            continue;

          // Since we have computed the area the centroid comes for free.
          // Centroids should also match
          if (planeF.ClosestPoint(planarFaces[g].Centroid).DistanceToSquared(centroidF) > RhinoMath.SqrtEpsilon)
            continue;

          // Add result to candidates List reversing index order to keep extrusion creation direction
          // Breps that come from a Extrusion have the Cap[0] before Cap[1]
          candidateFaces.Add(new int[] { g, f });
        }
      }

      // No twin faces found
      if (candidateFaces.Count == 0)
        return false;

      // Candidates are in 'LoopArea' order, we will find here smaller profiles sooner
      // This is good for beam like objects, bad for slab like objects.

      // On box-like Breps the result could be ambigous for the user so,
      // to give him some control on the result, we will prioritize first and last faces no matter their area.
      // First and Last are "special" because if somebody observe an extrusion-like Brep and sees
      // it as an extrusion he tends to categorize faces in one of the following schemas:
      // { Cap[0], Wall[0] .. Wall[N], Cap[1] }
      // { Cap[0], Cap[1], Wall[0] .. Wall[N] }
      // { Wall[0] .. Wall[N], Cap[0], Cap[1] }
      // So if he is using the join command to create a Brep from surfaces at the model,
      // it's natural to start or end the selection with one of the extrusion caps.
      // On horizontal extrusions, slab-like Breps, the user use to observe the model from above,
      // so probably he will end with the bottom cap.
      // Also Last face is a Cap in Breps that come from Extrusion
      // If caps and walls are interleaved, smallest pair of faces will be used as caps, producing beam-like extrusions.

      //  System.Linq.Enumerable.OrderBy performs a stable sort so only first and last face will be moved if found.
      foreach (var candidate in candidateFaces.OrderBy(pair => (planarFaces[pair[1]].Face.FaceIndex == brep.Faces.Count - 1) ? 0 : // Last,  in case it comes from Extrusion
                                                               (planarFaces[pair[0]].Face.FaceIndex == 0) ? 1 : // First, in case it comes from a JOIN command
                                                                                                                    int.MaxValue)) // Others
      {
        var startFace = planarFaces[candidate[0]];
        var endFace = planarFaces[candidate[1]];

        // If any face, ignorig twins candidates, does not degenrate
        // to a curve when projected to 'planeF', then brep is not an extrusion
        if
        (
          brep.Faces.
          Where(face => face.FaceIndex != startFace.Face.FaceIndex && face.FaceIndex != endFace.Face.FaceIndex).
          Any(face => !startFace.ProjectionDegenartesToCurve(face.UnderlyingSurface()))
        )
          return false;

        // We use the orginal OuterLoop as profile not the NURBS version of it
        // to keep the structure as much as possible
        var profile = startFace.Face.OuterLoop.To3dCurve();

        double height = startFace.Face.OrientationIsReversed ?
                        -startFace.Plane.DistanceTo(endFace.Plane.Origin) :
                        +startFace.Plane.DistanceTo(endFace.Plane.Origin);

        extrusion = Extrusion.Create(profile, height, true);
        var profilePlane = extrusion.GetProfilePlane(height < 0.0 ? 1.0 : 0.0);
        var WCSTOPCS = Transform.PlaneToPlane(profilePlane, Plane.WorldXY);
        foreach (var loop in startFace.Face.Loops.Where(x => x.LoopType == BrepLoopType.Inner))
        {
          var innerProfile = loop.To3dCurve();
          innerProfile.Transform(WCSTOPCS);
          extrusion.AddInnerProfile(innerProfile);
        }

        return extrusion is object;
      }

      return false;
    }
  }

  static class EllipseExtension
  {
    public static Ellipse Reverse(this Ellipse ellipse)
    {
      var plane = ellipse.Plane;
      plane.Flip();

      return new Ellipse(plane, ellipse.Radius2, ellipse.Radius1);
    }

    public static bool IsClosed(this Ellipse ellipse, Interval interval, double tolerance)
    {
      return ellipse.PointAt(interval.T0).DistanceTo(ellipse.PointAt(interval.T1)) < tolerance;
    }

    /// <summary>
    /// Finds parameter of the point on a curve that is closest to testPoint.
    /// </summary>
    /// <param name="ellipse"></param>
    /// <param name="testPoint">Point to search from.</param>
    /// <param name="t">Parameter of local closest point.</param>
    /// <returns>true on success, false on failure.</returns>
    public static bool ClosestPoint(this Ellipse ellipse, Point3d testPoint, out double t)
    {
      ellipse.Plane.ClosestParameter(testPoint, out var u, out var v);
      var uv = new Vector2d(u * ellipse.Radius2, v * ellipse.Radius1);

      if (uv.Unitize())
      {
        t = Math.Atan2(uv.Y, uv.X);
        return true;
      }
      else
      {
        t = double.NaN;
        return false;
      }
    }

    /// <summary>
    /// Evaluates point at a curve parameter.
    /// </summary>
    /// <param name="ellipse"></param>
    /// <param name="t">Evaluation parameter.</param>
    /// <returns>Point (location of curve at the parameter t).</returns>
    public static Point3d PointAt(this Ellipse ellipse, double t)
    {
      return ellipse.Plane.PointAt
      (
        ellipse.Radius1 * Math.Cos(t),
        ellipse.Radius2 * Math.Sin(t)
      );
    }
  }

  static class CurveExtension
  {
    /// <summary>
    /// Gets array of span "knots".
    /// </summary>
    /// <param name="curve"></param>
    /// <returns>An array with span vectors; or null on error.</returns>
    public static double[] GetSpanVector(this Curve curve)
    {
      var spanCount = curve.SpanCount;
      if (spanCount > 0)
      {
        var spanVector = new double[spanCount + 1];
        for (int s = 0; s < spanCount; ++s)
          spanVector[s] = curve.SpanDomain(s).T0;

        spanVector[spanCount] = curve.SpanDomain(spanCount - 1).T1;
        return spanVector;
      }

      return default;
    }

    /// <summary>
    /// Gets a value indicating whether or not this curve is a closed curve.
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="tolerance">Tolerance value used to compare start and end curve points.</param>
    /// <returns></returns>
    public static bool IsClosed(this Curve curve, double tolerance)
    {
      return curve.IsClosed || curve.PointAtStart.DistanceTo(curve.PointAtEnd) < tolerance;
    }

    /// <summary>
    /// Test a curve to see if it runs parallel to a specific plane.
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="plane">plane used to check for parallelism.</param>
    /// <param name="tolerance">Tolerance value used to check for planarity.</param>
    /// <param name="angleTolerance">Tolerance value used to check for coplanarity.</param>
    /// <returns></returns>
    /// <seealso cref="Rhino.Geometry.Curve.IsInPlane(Plane, double)"/>
    public static bool IsParallelToPlane(this Curve curve, Plane plane, double tolerance, double angleTolerance)
    {
      if (curve.TryGetLine(out var line, tolerance))
        return line.Direction.IsPerpendicularTo(plane.Normal, angleTolerance);

      return curve.TryGetPlane(out var curvePlane, tolerance) &&
        curvePlane.ZAxis.IsParallelTo(plane.Normal, angleTolerance) == 0;
    }

    /// <summary>
    /// Try to convert this curve into a <see cref="Rhino.Geometry.Line"/> using a custom <paramref name="tolerance"/>.
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="line">On success, the Line will be filled in.</param>
    /// <param name="tolerance">Tolerance to use when checking.</param>
    /// <returns>true if the curve could be converted into a Line within tolerance.</returns>
    public static bool TryGetLine(this Curve curve, out Line line, double tolerance)
    {
      if (curve is LineCurve lineCurve)
      {
        line = lineCurve.Line;
        return true;
      }
      else if (curve.IsLinear(tolerance))
      {
        line = new Line(curve.PointAtStart, curve.PointAtEnd);
        return true;
      }

      line = default;
      return false;
    }

    /// <summary>
    /// Try to convert this curve into a <see cref="Rhino.Geometry.Ellipse"/> using a custom <paramref name="tolerance"/>.
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="ellipse">On success, the Ellipse will be filled in.</param>
    /// <param name="domain">On success, the Ellipse domain will be filled in.</param>
    /// <param name="tolerance">Tolerance to use when checking.</param>
    /// <returns>true if the curve could be converted into a Ellipse within tolerance.</returns>
    public static bool TryGetEllipse(this Curve curve, out Ellipse ellipse, out Interval domain, double tolerance)
    {
      if (curve.TryGetPlane(out var plane, tolerance))
      {
        if (curve.TryGetArc(plane, out var arc, tolerance))
        {
          ellipse = new Ellipse(arc.Plane, arc.Radius, arc.Radius);
          domain = arc.AngleDomain;
          return true;
        }

        return curve.TryGetEllipse(plane, out ellipse, out domain, tolerance);
      }

      ellipse = default;
      domain = Interval.Unset;
      return false;
    }

    /// <summary>
    /// Try to convert this curve into a <see cref="Rhino.Geometry.Ellipse"/> using a custom <paramref name="tolerance"/>.
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="plane">Plane in which the comparison is performed.</param>
    /// <param name="ellipse">On success, the Ellipse will be filled in.</param>
    /// <param name="domain">On success, the Ellipse domain will be filled in.</param>
    /// <param name="tolerance">Tolerance to use when checking.</param>
    /// <returns>true if the curve could be converted into a Ellipse within tolerance.</returns>
    public static bool TryGetEllipse(this Curve curve, Plane plane, out Ellipse ellipse, out Interval domain, double tolerance)
    {
      if (curve.TryGetEllipse(plane, out ellipse, tolerance))
      {
        // Curve.TryGetEllipse does not honor curve direction
        if(curve.ClosedCurveOrientation(ellipse.Plane) == CurveOrientation.Clockwise)
          ellipse = ellipse.Reverse();

        if (curve.IsClosed)
        {
          domain = new Interval(0.0, 2.0 * Math.PI);
          return true;
        }
        else
        {
          ellipse.ClosestPoint(curve.PointAtStart, out var t0);
          ellipse.ClosestPoint(curve.PointAtEnd, out var t1);
          domain = new Interval(t0, t1);
          return true;
        }
      }

      ellipse = default;
      domain = Interval.Unset;
      return false;
    }

    internal static bool TryGetHermiteSpline(this Curve curve, out IList<Point3d> points, out Vector3d startTangent, out Vector3d endTangent, double tolerance)
    {
      if (curve.Fit(3, tolerance * 0.2, 0.0) is NurbsCurve fit)
      {
        var interval = curve.Domain;
        startTangent = curve.TangentAt(interval.T0);
        endTangent = curve.TangentAt(interval.T1);
        points = fit.GrevillePoints(true);

        return true;
      }
      else
      {
        startTangent = default;
        endTangent = default;
        points = default;

        return false;
      }
    }

    /// <summary>
    /// Try to convert this curve into a <see cref="Rhino.Geometry.PolyCurve"/> using a custom <paramref name="angleToleranceRadians"/>.
    /// </summary>
    /// <remarks>
    /// It splits <paramref name="curve"/> at kinks, and creates a PolyCurve with the resulting G2 continuous segments.
    /// </remarks>
    /// <param name="curve"></param>
    /// <param name="polyCurve">Resulting polycurve of smooth spans.</param>
    /// <param name="angleToleranceRadians">Tolerance to use when checking for kinks, in radians.</param>
    /// <returns>true if the curve has kinks within tolerance and results into a PolyCurve.</returns>
    public static bool TryGetPolyCurve(this Curve curve, out PolyCurve polyCurve, double angleToleranceRadians)
    {
      var kinks = default(List<double>);

      var continuity = curve.IsClosed ? Continuity.G2_locus_continuous : Continuity.G2_continuous;
      var domain = curve.Domain;
      var t = domain.T0;
      var cosAngleTolerance = Math.Cos(angleToleranceRadians);

      while (curve.GetNextDiscontinuity(continuity, t, domain.T1, cosAngleTolerance, out t))
      {
        if (kinks is null) kinks = new List<double>();
        kinks.Add(t);
      }

      if (kinks is null)
      {
        polyCurve = default;
        return false;
      }

      polyCurve = new PolyCurve();
      foreach (var segment in curve.Split(kinks))
        polyCurve.AppendSegment(segment);

      return true;
    }

    static bool TryEvaluateCurvature
    (
      Vector3d D1,
      Vector3d D2,
      out Vector3d T,
      out Vector3d K
    )
    {
      double d1 = D1.Length;
      if (d1 == 0.0)
      {
        d1 = D2.Length;
        if (d1 > 0.0) T = D2 / d1;
        else T = Vector3d.Zero;

        K = Vector3d.Zero;
        return false;
      }

      T = D1 / d1;

      double negD2oT = -D2 * T;
      d1 = 1.0 / (d1 * d1);
      K = d1 * (D2 + negD2oT * T);
      return true;
    }

    public static bool GetNextDiscontinuity(this Curve curve, Continuity continuityType, double t0, double t1, double cosAngleTolerance, out double t)
    {
      return curve.GetNextDiscontinuity(continuityType, t0, t1, cosAngleTolerance, RhinoMath.SqrtEpsilon, out t);
    }

    public static bool GetNextDiscontinuity(this Curve curve, Continuity continuityType, double t0, double t1, double cosAngleTolerance, double curvatureTolerance, out double t)
    {
      var derivatives = 0;
      switch (continuityType)
      {
        case Continuity.G1_continuous:        derivatives = 1; continuityType = Continuity.C1_continuous; break;
        case Continuity.G1_locus_continuous:  derivatives = 1; continuityType = Continuity.C1_locus_continuous; break;
        case Continuity.G2_continuous:        derivatives = 2; continuityType = Continuity.C2_continuous; break;
        case Continuity.G2_locus_continuous:  derivatives = 2;  continuityType = Continuity.C2_locus_continuous; break;
        default: return curve.GetNextDiscontinuity(continuityType, t0, t1, out t);
      }

      while (curve.GetNextDiscontinuity(continuityType, t0, t1, out t))
      {
        t0 = t;
        var domain = curve.Domain;

        var below = t == domain.T1 ?
          curve.DerivativeAt(domain.T1, 2) :
          curve.DerivativeAt(t, 2, CurveEvaluationSide.Below);

        TryEvaluateCurvature(below[1], below[2], out var belowT, out var belowK);

        var above = t == domain.T1 ?
          curve.DerivativeAt(domain.T0, 2) :
          curve.DerivativeAt(t, 2, CurveEvaluationSide.Above);

        TryEvaluateCurvature(above[1], above[2], out var aboveT, out var aboveK);

        // Check if is G1 continuous
        if (belowT * aboveT < cosAngleTolerance)
          return true;

        // Check if is G2 continuous
        if(derivatives >= 2)
        {
          if ((belowK - aboveK).Length > curvatureTolerance)
            return true;

          var belowKLength = belowK.Length;
          var aboveKLength = aboveK.Length;
          if (Math.Abs(belowKLength - aboveKLength) > Math.Max(belowKLength, aboveKLength) * 0.05)
            return true;
        }
      }

      return false;
    }
  }

  static class GeometryBaseExtension
  {
    public static bool TryGetUserString<T>(this GeometryBase geometry, string key, out T value, T def) where T : IConvertible
    {
      if (geometry.GetUserString(key) is string stringValue)
      {
        if (typeof(T).IsEnum)
          value = (T) Enum.Parse(typeof(T), stringValue);
        else
          value = (T) System.Convert.ChangeType(stringValue, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
        return true;
      }

      value = def;
      return false;
    }

    public static bool TrySetUserString<T>(this GeometryBase geometry, string key, T value, T def) where T : IConvertible
    {
      if (value.Equals(def))
        return geometry.SetUserString(key, null);

      var stringValue = (string) System.Convert.ChangeType(value, typeof(string), System.Globalization.CultureInfo.InvariantCulture);
      return geometry.SetUserString(key, stringValue);
    }

    public static bool TryGetUserString(this GeometryBase geometry, string key, out Autodesk.Revit.DB.ElementId value) =>
      TryGetUserString(geometry, key, out value, Autodesk.Revit.DB.ElementId.InvalidElementId);

    public static bool TryGetUserString(this GeometryBase geometry, string key, out Autodesk.Revit.DB.ElementId value, Autodesk.Revit.DB.ElementId def)
    {
      if (geometry.TryGetUserString(key, out int id, def.IntegerValue))
      {
        value = new Autodesk.Revit.DB.ElementId(id);
        return true;
      }

      value = def;
      return false;
    }

    public static bool TrySetUserString(this GeometryBase geometry, string key, Autodesk.Revit.DB.ElementId value) =>
      geometry.TrySetUserString(key, value.IntegerValue, Autodesk.Revit.DB.ElementId.InvalidElementId.IntegerValue);

    public static bool TrySetUserString(this GeometryBase geometry, string key, Autodesk.Revit.DB.ElementId value, Autodesk.Revit.DB.ElementId def) =>
      geometry.TrySetUserString(key, value.IntegerValue, def.IntegerValue);
  }
}

namespace Rhino.DocObjects.Tables
{
  static class NamedConstructionPlaneTableExtension
  {
    public static int Add(this NamedConstructionPlaneTable table, ConstructionPlane cplane)
    {
      if (table.Document != RhinoDoc.ActiveDoc)
        throw new InvalidOperationException("Invalid Rhino Active Document");

      if (table.Find(cplane.Name) < 0)
      {
        var previous = table.Document.Views.ActiveView.MainViewport.GetConstructionPlane();

        try
        {
          table.Document.Views.ActiveView.MainViewport.SetConstructionPlane(cplane);
          //table.Document.Views.ActiveView.MainViewport.PushConstructionPlane(cplane);
          if (RhinoApp.RunScript($"_-NamedCPlane _Save \"{cplane.Name}\" _Enter", false))
            return table.Count;
        }
        finally
        {
          //table.Document.Views.ActiveView.MainViewport.PopConstructionPlane();
          table.Document.Views.ActiveView.MainViewport.SetConstructionPlane(previous);
        }
      }

      return -1;
    }

    public static bool Modify(this NamedConstructionPlaneTable table, ConstructionPlane cplane, int index, bool quiet)
    {
      if (table.Document != RhinoDoc.ActiveDoc)
        throw new InvalidOperationException("Invalid Rhino Active Document");

      if (index <= table.Count)
      {
        var previous = table.Document.Views.ActiveView.MainViewport.GetConstructionPlane();

        try
        {
          //table.Document.Views.ActiveView.MainViewport.PushConstructionPlane(cplane);
          table.Document.Views.ActiveView.MainViewport.SetConstructionPlane(cplane);

          var current = table[index];
          if (current.Name != cplane.Name)
            return RhinoApp.RunScript($"_-NamedCPlane _Rename \"{current.Name}\" \"{cplane.Name}\" _Save \"{cplane.Name}\" _Enter", !quiet);
          else
            return RhinoApp.RunScript($"_-NamedCPlane _Save \"{cplane.Name}\" _Enter", !quiet);
        }
        finally
        {
          //table.Document.Views.ActiveView.MainViewport.PopConstructionPlane();
          table.Document.Views.ActiveView.MainViewport.SetConstructionPlane(previous);
        }
      }

      return false;
    }
  }
}

namespace Rhino.Display
{
  static class RhinoViewExtension
  {
    public static bool BringToFront(this RhinoView view)
    {
      var viewWindow = new WindowHandle(view.Handle);
      if (!viewWindow.IsZero)
      {
        var topMost = viewWindow;
        while (!topMost.Parent.IsZero)
        {
          topMost = topMost.Parent;
          //if (view.Floating) break;
          if (!viewWindow.Parent.Owner.IsZero) break;
        }

        if (topMost.Visible == false) topMost.Visible = true;
        return topMost.BringToFront();
      }

      return false;
    }
  }

  static class RhinoViewportExtension
  {
    public static bool SetViewProjection(this RhinoViewport viewport, DocObjects.ViewportInfo camera, bool updateTargetLocation, bool updateScreenPort)
    {
      var vportInfo = camera;

      if (updateScreenPort)
      {
        viewport.GetScreenPort(out var left, out var right, out var top, out var bottom, out var _, out var _);
        vportInfo = new DocObjects.ViewportInfo(camera)
        {
          FrustumAspect = viewport.FrustumAspect,
          ScreenPort = new System.Drawing.Rectangle(left, top, right - left, top - bottom)
        };
      }

      if (viewport.SetViewProjection(vportInfo, updateTargetLocation))
      {
        if (updateTargetLocation)
        {
          if (camera.TargetPoint.IsValid)
            viewport.SetCameraTarget(camera.TargetPoint, false);
          else
            viewport.SetCameraTarget(vportInfo.CameraLocation + vportInfo.CameraDirection, false);
        }

        return true;
      }

      return false;
    }
  }
}
