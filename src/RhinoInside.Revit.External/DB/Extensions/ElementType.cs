using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class ElementTypeExtensions
  {
    public static string GetFamilyName(this ElementType elementType) => UI.HostedApplication.Active.InvokeInHostContext(() => elementType.FamilyName);
  }
}
