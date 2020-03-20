using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  class FamilyLoadOptions : DB.IFamilyLoadOptions
  {
    readonly bool OverrideFamily;
    readonly bool OverrideParameters;

    public FamilyLoadOptions(bool overrideFamily, bool overrideParameters)
    {
      OverrideFamily = overrideFamily;
      OverrideParameters = overrideParameters;
    }

    bool DB.IFamilyLoadOptions.OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
    {
      overwriteParameterValues = !familyInUse | OverrideParameters;
      return !familyInUse | OverrideFamily;
    }

    bool DB.IFamilyLoadOptions.OnSharedFamilyFound(DB.Family sharedFamily, bool familyInUse, out DB.FamilySource source, out bool overwriteParameterValues)
    {
      source = DB.FamilySource.Family;
      overwriteParameterValues = !familyInUse | OverrideParameters;
      return !familyInUse | OverrideFamily;
    }
  }
}
