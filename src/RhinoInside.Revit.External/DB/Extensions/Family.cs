using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class FamilyExtension
  {
    public static FamilyHostingBehavior GetHostingBehavior(this Family family)
    {
      return family.get_Parameter(BuiltInParameter.FAMILY_HOSTING_BEHAVIOR).AsEnum<FamilyHostingBehavior>();
    }
  }
}
