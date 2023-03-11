using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class FamilyExtension
  {
    public static FamilyHostingBehavior GetHostingBehavior(this Family family)
    {
      return family.get_Parameter(BuiltInParameter.FAMILY_HOSTING_BEHAVIOR).AsEnum<FamilyHostingBehavior>();
    }

    public static bool IsWorkPlaneBased(this Family family)
    {
      return family.get_Parameter(BuiltInParameter.FAMILY_WORK_PLANE_BASED)?.AsBoolean() ?? false;
    }

    public static bool SetWorkPlaneBased(this Family family, bool value)
    {
      using (var workPlaneBasedParameter = family.get_Parameter(BuiltInParameter.FAMILY_WORK_PLANE_BASED))
      {
        if (workPlaneBasedParameter?.IsReadOnly is false)
          return workPlaneBasedParameter.Update(value);
      }

      return false;
    }
  }
}
