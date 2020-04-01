namespace RhinoInside.Revit.Extended
{
  using static System.Math;
  using static System.Double;

  public static class Math
  {
    //public static int Clamp(this int v, int lo, int hi)
    //{
    //  return hi < v ? hi : v < lo ? lo : v;
    //}

    //public static double Clamp(this double v, double lo, double hi)
    //{
    //  return hi < v ? hi : v < lo ? lo : v;
    //}

    public static bool IsPositive(double value)
    {
      switch (Sign(value))
      {
        case -1: return false;
        case +1: return true;
      }

      return IsPositiveInfinity(1.0 / value);
    }

    public static bool IsNegative(double value)
    {
      switch (Sign(value))
      {
        case -1: return true;
        case +1: return false;
      }

      return IsNegativeInfinity(1.0 / value);
    }
  }
}
