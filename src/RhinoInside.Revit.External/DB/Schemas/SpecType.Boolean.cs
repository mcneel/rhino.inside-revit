using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Schemas
{
  public partial class SpecType
  {
    public static class Boolean
    {
      public static SpecType YesNo => new SpecType("autodesk.spec:spec.bool-1.0.0");
    }
  }
}
