using System.Diagnostics;
using System.Linq;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry
{
  /// <summary>
  /// Converts <see cref="NurbsCurve"/> to be transfered to a <see cref="DB.NurbSpline"/>.
  /// <para>Non C2 curves are "Smoothed" when necessary.</para>
  /// </summary>
  static class NurbsSplineEncoder
  {
    static bool KnotAlmostEqualTo(double max, double min) =>
      KnotAlmostEqualTo(max, min, 1.0e-09);

    static bool KnotAlmostEqualTo(double max, double min, double tol)
    {
      var length = max - min;
      if (length <= tol)
        return true;

      return length <= max * tol;
    }

    static double KnotPrevNotEqual(double max) =>
      KnotPrevNotEqual(max, 1.0000000E-9 * 1000.0);

    static double KnotPrevNotEqual(double max, double tol)
    {
      const double delta2 = 2.0 * 1E-16;
      var value = max - tol - delta2;

      if (!KnotAlmostEqualTo(max, value, tol))
        return value;

      return max - (max * (tol + delta2));
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

        double next = knots[k + 2];
        if (KnotAlmostEqualTo(next, current))
        {
          multiplicity++;
          if (multiplicity > degree - 2)
            current = KnotPrevNotEqual(next);
          else
            current = next;
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

    static DB.XYZ[] ToXYZArray(NurbsCurvePointList list, double factor)
    {
      var count = list.Count;
      var points = new DB.XYZ[count];

      int p = 0;
      if (factor == 1.0)
      {
        while (p < count)
        {
          var location = list[p].Location;
          points[p++] = new DB::XYZ(location.X, location.Y, location.Z);
        }
      }
      else
      {
        while (p < count)
        {
          var location = list[p].Location;
          points[p++] = new DB::XYZ(location.X * factor, location.Y * factor, location.Z * factor);
        }
      }

      return points;
    }

    internal static DB.Curve ToNurbsSpline(NurbsCurve value, double factor)
    {
      var degree = value.Degree;
      var knots = ToDoubleArray(value.Knots, degree);
      var controlPoints = ToXYZArray(value.Points, factor);

      Debug.Assert(degree > 2 || value.SpanCount == 1);
      Debug.Assert(degree >= 1);
      Debug.Assert(controlPoints.Length > degree);
      Debug.Assert(knots.Length == degree + controlPoints.Length + 1);

      if (value.IsRational)
      {
        var weights = value.Points.Select(p => p.Weight).ToArray();
        return DB.NurbSpline.CreateCurve(value.Degree, knots, controlPoints, weights);
      }
      else
      {
        return DB.NurbSpline.CreateCurve(value.Degree, knots, controlPoints);
      }
    }
  }
}
