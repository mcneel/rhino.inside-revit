using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  class FamilyLoadOptions : DB.IFamilyLoadOptions
  {
    readonly bool OverwriteFamily;
    readonly bool OverwriteParameters;

    public FamilyLoadOptions(bool overwriteFamily, bool overwriteParameters)
    {
      OverwriteFamily = overwriteFamily;
      OverwriteParameters = overwriteParameters;
    }

    bool DB.IFamilyLoadOptions.OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
    {
      overwriteParameterValues = !familyInUse | OverwriteParameters;
      return !familyInUse | OverwriteFamily;
    }

    bool DB.IFamilyLoadOptions.OnSharedFamilyFound(DB.Family sharedFamily, bool familyInUse, out DB.FamilySource source, out bool overwriteParameterValues)
    {
      source = DB.FamilySource.Family;
      overwriteParameterValues = !familyInUse | OverwriteParameters;
      return !familyInUse | OverwriteFamily;
    }
  }
}
