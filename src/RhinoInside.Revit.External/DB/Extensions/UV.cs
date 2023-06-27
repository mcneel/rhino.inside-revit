using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class UVExtension
  {
    public static UV NaN { get; } = null; // new UV(double.NaN, double.NaN);
    public static UV Zero { get; } = UV.Zero;
    public static UV BasisU { get; } = UV.BasisU;
    public static UV BasisV { get; } = UV.BasisV;

    //public static UV NegativeInfinity { get; } = new UV(double.NegativeInfinity, double.NegativeInfinity);
    //public static UV PositiveInfinity { get; } = new UV(double.PositiveInfinity, double.PositiveInfinity);

    public static UV MinValue { get; } = new UV(double.MinValue, double.MinValue);
    public static UV MaxValue { get; } = new UV(double.MaxValue, double.MaxValue);

    public static void Deconstruct
    (
      this UV value,
      out double u, out double v
    )
    {
      u = value.U;
      v = value.V;
    }
  }
}
