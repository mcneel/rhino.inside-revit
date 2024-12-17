namespace Autodesk.Revit.DB.Structure
{
#if !REVIT_2023
  public enum AnalyticalStructuralRole
  {
    Unset = -1,
    StructuralRoleBeam = 0,
    StructuralRoleColumn = 1,
    StructuralRoleMember = 3,
    StructuralRoleGirder = 4,
    StructuralRoleFloor = 5,
    StructuralRoleWall = 6,
    StructuralRolePanel = 7
  }
#endif
}
