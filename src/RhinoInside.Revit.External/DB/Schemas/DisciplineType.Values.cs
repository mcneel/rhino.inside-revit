using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Schemas
{
  public partial class DisciplineType
  {
    public static DisciplineType Architecture => new DisciplineType("autodesk.spec.discipline:architecture-1.0.0");
    public static DisciplineType Common => new DisciplineType("autodesk.spec:discipline-1.0.0");
    public static DisciplineType Electrical => new DisciplineType("autodesk.spec.discipline:electrical-1.0.0");
    public static DisciplineType Energy => new DisciplineType("autodesk.spec.discipline:energy-1.0.0");
    public static DisciplineType Hvac => new DisciplineType("autodesk.spec.discipline:hvac-1.0.0");
    public static DisciplineType Infrastructure => new DisciplineType("autodesk.spec.discipline:infrastructure-1.0.0");
    public static DisciplineType Piping => new DisciplineType("autodesk.spec.discipline:piping-1.0.0");
    public static DisciplineType Structural => new DisciplineType("autodesk.spec.discipline:structural-1.0.0");
  }
}
