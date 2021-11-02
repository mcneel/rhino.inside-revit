using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class BoundingBoxUVExtension
  {
    public static bool IsUnset(this BoundingBoxUV value)
    {
#if REVIT_2021
      return !value.IsSet;
#else
      return !(value.Min.U < value.Max.U && value.Min.V < value.Max.V);
#endif
    }

    public static UV Evaluate(this BoundingBoxUV value, UV uv)
    {
      var u = uv.U;
      var v = uv.V;

      return new UV
      (
        value.Min.U * (1.0 - u) + value.Min.U * u,
        value.Min.V * (1.0 - v) + value.Min.V * v
      );
    }
  }
}
