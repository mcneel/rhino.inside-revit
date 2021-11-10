using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Schemas
{
  public partial class SpecType
  {
    public static class Int
    {
      public static SpecType Integer => new SpecType("autodesk.spec:spec.int64-2.0.0");
      public static SpecType NumberOfPoles => new SpecType("autodesk.spec.aec:numberOfPoles-2.0.0");
    }
  }
}
