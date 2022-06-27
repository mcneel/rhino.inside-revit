using System.Diagnostics;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry
{
  using Convert.System.Collections.Generic;
  using External.DB;

  /// <summary>
  /// Converts <see cref="NurbsCurve"/> to be transfered to a <see cref="ARDB.NurbSpline"/>.
  /// <para>Non C2 curves are "Smoothed" when necessary.</para>
  /// </summary>
  static class NurbsSplineEncoder
  {
    static bool NormalizedKnotAlmostEqualTo(double max, double min, double tol = NumericTolerance.DefaultTolerance)
    {
      Debug.Assert(0.0 <= min && min <= max && max <= 1.0);

      return max - min <= tol;
    }

    static double KnotPrevNotEqual(double max, double tol = NumericTolerance.DefaultTolerance)
    {
      Debug.Assert(tol >= NumericTolerance.DefaultTolerance);

      return max - (2.0 * tol);
    }

    static double[] ToDoubleArray(NurbsCurveKnotList list, int degree)
    {
      var count = list.Count;
      var knots = new double[count + 2];

      var min = list[0];
      var max = list[count - 1];
      var mid = 0.5 * (min + max);
      var factor = 1.0 / (max - min); // normalized

      // End knot
      knots[count + 1] = /*(list[count - 1] - max) * factor +*/ 1.0;
      for (int k = count - 1; k >= count - degree; --k)
        knots[k + 1] = /*(list[k] - max) * factor +*/ 1.0;

      // Interior knots (in reverse order)
      int multiplicity = degree + 1;
      for (int k = count - degree - 1; k >= degree; --k)
      {
        double current = list[k] <= mid ?
          (list[k] - min) * factor + 0.0:
          (list[k] - max) * factor + 1.0;

        double next = list[k+1] <= mid ?
          (list[k+1] - min) * factor + 0.0:
          (list[k+1] - max) * factor + 1.0;

        if (NormalizedKnotAlmostEqualTo(next, current))
        {
          multiplicity++;
          if (multiplicity > degree - 2)
            current = KnotPrevNotEqual(knots[k+2]);
          else
            current = knots[k+2];
        }
        else multiplicity = 1;

        knots[k + 1] = current;
      }

      // Start knot
      for (int k = degree - 1; k >= 0; --k)
        knots[k + 1] = /*(list[k] - min) * factor +*/ 0.0;
      knots[0] = /*(list[0] - min) * factor +*/ 0.0;

      return knots;
    }

    static ARDB.XYZ[] ToXYZArray(NurbsCurvePointList list, double factor)
    {
      var count = list.Count;
      var points = new ARDB.XYZ[count];

      int p = 0;
      if (factor == 1.0)
      {
        while (p < count)
        {
          var location = list[p].Location;
          points[p++] = new ARDB::XYZ(location.X, location.Y, location.Z);
        }
      }
      else
      {
        while (p < count)
        {
          var location = list[p].Location;
          points[p++] = new ARDB::XYZ(location.X * factor, location.Y * factor, location.Z * factor);
        }
      }

      return points;
    }

    internal static ARDB.Curve ToNurbsSpline(NurbsCurve value, double factor)
    {
      var degree = value.Degree;
      var knots = ToDoubleArray(value.Knots, degree);
      var controlPoints = ToXYZArray(value.Points, factor);

      Debug.Assert(degree > 2 || value.SpanCount == 1);
      Debug.Assert(degree >= 1);
      Debug.Assert(controlPoints.Length > degree);
      Debug.Assert(knots.Length == (degree + 1) + controlPoints.Length);

      if (value.IsRational)
      {
        var weights = value.Points.ConvertAll(x => x.Weight);
        return ARDB.NurbSpline.CreateCurve(degree, knots, controlPoints, weights);
      }
      else
      {
        return ARDB.NurbSpline.CreateCurve(degree, knots, controlPoints);
      }
    }
  }
}
