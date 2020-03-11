using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace RhinoInside.Revit.GH.Components.Documents.Families
{
  class FamilyLoadOptions : DB.IFamilyLoadOptions
  {
    public FamilyLoadOptions(bool overrideFamily, bool overrideParameters)
    {
      OverrideFamily = overrideFamily;
      OverrideParameters = overrideParameters;
    }

    readonly bool OverrideFamily;
    readonly bool OverrideParameters;

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
