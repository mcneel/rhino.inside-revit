using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Schemas
{
  public partial class SpecType
  {
    public static class String
    {
      public static SpecType MultilineText => new SpecType("autodesk.spec.aec:multilineText-2.0.0");
      public static SpecType Text => new SpecType("autodesk.spec:spec.string-2.0.0");
      public static SpecType Url => new SpecType("autodesk.spec.string:url-2.0.0");
    }
  }
}
