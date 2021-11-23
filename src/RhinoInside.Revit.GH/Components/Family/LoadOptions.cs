using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Families
{
  class FamilyLoadOptions : ARDB.IFamilyLoadOptions
  {
    readonly bool OverwriteFamily;
    readonly bool OverwriteParameters;

    public FamilyLoadOptions(bool overwriteFamily, bool overwriteParameters)
    {
      OverwriteFamily = overwriteFamily;
      OverwriteParameters = overwriteParameters;
    }

    bool ARDB.IFamilyLoadOptions.OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
    {
      overwriteParameterValues = !familyInUse | OverwriteParameters;
      return !familyInUse | OverwriteFamily;
    }

    bool ARDB.IFamilyLoadOptions.OnSharedFamilyFound(ARDB.Family sharedFamily, bool familyInUse, out ARDB.FamilySource source, out bool overwriteParameterValues)
    {
      source = ARDB.FamilySource.Family;
      overwriteParameterValues = !familyInUse | OverwriteParameters;
      return !familyInUse | OverwriteFamily;
    }
  }
}
