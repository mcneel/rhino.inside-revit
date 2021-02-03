using System.Collections.Generic;
using Rhino.Geometry.Collections;

namespace RhinoInside.Revit.Convert.Geometry
{
  static class KnotListEncoder
  {
    public const double KnotTolerance = 1e-9;

    /// <summary>
    /// Compares a knot <paramref name="value"/> with a <paramref name="successor"/> value,
    /// in a monotonic nondecreasing serie, for equality.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="successor"></param>
    /// <param name="tolerance"></param>
    /// <param name="strict">true in case <paramref name="value"/> and <paramref name="successor"/> are strictly equal, else false is returned.</param>
    /// <returns>true if <paramref name="value"/> and <paramref name="successor"/> are equal within <paramref name="tolerance"/>.</returns>
    public static bool KnotEqualTo(double value, double successor, double tolerance, out bool strict)
    {
      var distance = successor - value;
      if (distance <= tolerance)
      {
        strict = value == successor;
        return true;
      }

      strict = false;
      return distance <= successor * tolerance;
    }

    /// <summary>
    /// Get knot multiplicity.
    /// </summary>
    /// <param name="knots"></param>
    /// <param name="index">Index of knot to query.</param>
    /// <param name="tolerance"></param>
    /// <param name="average"></param>
    /// <param name="strict"></param>
    /// <returns>The multiplicity (valence) of the knot.</returns>
    public static int KnotMultiplicity(NurbsCurveKnotList knots, int index, double tolerance, out double average, out bool strict)
    {
      var i = index;
      var value = knots[i++];
      average = value;

      strict = true;
      while (i < knots.Count && KnotEqualTo(value, knots[i], tolerance, out var s))
      {
        strict &= s;
        average += knots[i];
        i++;
      }

      var multiplicity = i - index;

      if (strict) average = knots[index];
      else average /= multiplicity;

      return multiplicity;
    }

    /// <summary>
    /// Get knot multiplicity.
    /// </summary>
    /// <param name="knots"></param>
    /// <param name="index">Index of knot to query.</param>
    /// <param name="tolerance"></param>
    /// <param name="average"></param>
    /// <param name="strict"></param>
    /// <returns>The multiplicity (valence) of the knot.</returns>
    public static int KnotMultiplicity(NurbsSurfaceKnotList knots, int index, double tolerance, out double average, out bool strict)
    {
      var i = index;
      var value = knots[i++];
      average = value;

      strict = true;
      while (i < knots.Count && KnotEqualTo(value, knots[i], tolerance, out var s))
      {
        strict &= s;
        average += knots[i];
        i++;
      }

      var multiplicity = i - index;

      if (strict) average = knots[index];
      else average /= multiplicity;

      return multiplicity;
    }

    public static bool TryGetKinks(NurbsCurveKnotList knots, int degree, out List<double> kinks, double tolerance, out bool strict)
    {
      kinks = default;
      strict = true;

      for (int k = degree; k < knots.Count - degree;)
      {
        var multiplicity = KnotMultiplicity(knots, k, tolerance, out var average, out var s);
        strict &= s;

        if (!s)
        {
          for (int i = k; i < k + multiplicity; ++i)
            knots[i] = average;
        }

        if (multiplicity > degree - 2)
        {
          if (kinks is null) kinks = new List<double>();
          kinks.Add(average);
        }

        k += multiplicity;
      }

      return !(kinks is null);
    }

    public static bool TryGetKinks(NurbsSurfaceKnotList knots, int degree, out List<double> kinks, double tolerance, out bool strict)
    {
      kinks = default;
      strict = true;

      for (int k = degree; k < knots.Count - degree;)
      {
        var multiplicity = KnotMultiplicity(knots, k, tolerance, out var average, out var s);
        strict &= s;

        if (!s)
        {
          for (int i = k; i < k + multiplicity; ++i)
            knots[i] = average;
        }

        if (multiplicity > degree)
        {
          if (kinks is null) kinks = new List<double>();
          kinks.Add(average);
        }

        k += multiplicity;
      }

      return !(kinks is null);
    }
  }
}
