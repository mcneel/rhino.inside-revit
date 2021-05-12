using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Schemas
{
  public partial class SpecType
  {
    public static class Reference
    {
      public static SpecType Image => new SpecType("autodesk.spec.reference:image-1.0.0");
      public static SpecType LoadClassification => new SpecType("autodesk.spec.aec.electrical:loadClassification-1.0.0");
      public static SpecType Material => new SpecType("autodesk.spec.aec:material-1.0.0");
    }
  }
}
