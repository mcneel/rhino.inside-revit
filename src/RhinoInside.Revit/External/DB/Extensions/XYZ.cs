using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class XYZExtension
  {
    /// <summary>
    /// Checks if the the given two vectors are parallel
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="tolerance"></param>
    /// <returns>true if <paramref name="a"/> and <paramref name="b"/> are parallel</returns>
    public static bool IsParallelTo(this XYZ a, XYZ b, double tolerance = 1e-9)
    {
      var A = a.Normalize();
      var B = b.Normalize();

      return A.IsAlmostEqualTo(A.DotProduct(B) < 0.0 ? -B : B, tolerance);
    }

    /// <summary>
    /// Checks if the the given two vectors are codirectional
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="tolerance"></param>
    /// <returns>true if <paramref name="a"/> and <paramref name="b"/> are codirectional</returns>
    public static bool IsCodirectionalTo(this XYZ a, XYZ b, double tolerance = 1e-9)
    {
      var A = a.Normalize();
      var B = b.Normalize();

      return A.IsAlmostEqualTo(B, tolerance);
    }

    /// <summary>
    /// Checks if the the given two vectors are perpendicular
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="tolerance"></param>
    /// <returns>true if <paramref name="a"/> and <paramref name="b"/> are perpendicular</returns>
    public static bool IsPerpendicularTo(this XYZ a, XYZ b, double tolerance = 1e-9)
    {
      var A = a.Normalize();
      var B = b.Normalize();

      return A.DotProduct(B) < tolerance;
    }

    /// <summary>
    /// Arbitrary Axis Algorithm
    /// <para>Given a vector to be used as the Z axis of a coordinate system, this algorithm generates a corresponding X axis for the coordinate system.</para>
    /// <para>The Y axis follows by application of the right-hand rule.</para>
    /// </summary>
    /// <param name="value"></param>
    /// <param name="tolerance"></param>
    /// <returns>X axis of the corresponding coordinate system</returns>
    public static XYZ PerpVector(this XYZ value, double tolerance = 1e-9)
    {
      if (XYZ.Zero.IsAlmostEqualTo(new XYZ(value.X, value.Y, 0.0), tolerance))
        return new XYZ(value.Z, 0.0, -value.X);
      else
        return new XYZ(-value.Y, value.X, 0.0);
    }
  }
}
