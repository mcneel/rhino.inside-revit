using System;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class CADLinkTypeExtension
  {
    public static CADLinkType GetCADLinkType(this Document doc, string name, bool linked)
    {
      using (var linkTypes = new FilteredElementCollector(doc).OfClass(typeof(CADLinkType)))
      {
        foreach (var linkType in linkTypes.Cast<CADLinkType>())
        {
          if (linkType.IsExternalFileReference() != linked)
            continue;

          if (ElementNaming.NameEqualityComparer.Equals(linkType.Name, name))
            return linkType;
        }
      }

      return null;
    }

    public static CADLinkType GetCADLinkedType(this Document doc, string path)
    {
      using (var linkTypes = new FilteredElementCollector(doc).OfClass(typeof(CADLinkType)))
      {
        foreach (var linkType in linkTypes.Cast<CADLinkType>())
        {
          if (!linkType.IsExternalFileReference())
            continue;

          var extRef = linkType.GetExternalFileReference();
          if (extRef == null)
            continue;

          var currentLinkPath = ModelPathUtils.ConvertModelPathToUserVisiblePath(extRef.GetAbsolutePath());
          if (string.Equals(currentLinkPath, path, StringComparison.OrdinalIgnoreCase))
            return linkType;
        }
      }

      return null;
    }

    public static ProjectLocation[] GetSharedSites(this CADLinkType type)
    {
      return type.GetDependents<ProjectLocation>();
    }

    public static void SetImportUnits(this CADLinkType type, ImportUnit unit, double customScale)
    {
      switch (unit)
      {
        case ImportUnit.Default: break;
        case ImportUnit.Inch:
          type.get_Parameter(BuiltInParameter.IMPORT_DISPLAY_UNITS).Set(6);
          type.get_Parameter(BuiltInParameter.IMPORT_SCALE).Set(1.0);
          break;
        case ImportUnit.Foot:
          type.get_Parameter(BuiltInParameter.IMPORT_DISPLAY_UNITS).Set(3);
          type.get_Parameter(BuiltInParameter.IMPORT_SCALE).Set(1.0);
          break;
        case ImportUnit.Millimeter:
          type.get_Parameter(BuiltInParameter.IMPORT_DISPLAY_UNITS).Set(2);
          type.get_Parameter(BuiltInParameter.IMPORT_SCALE).Set(1.0);
          break;
        case ImportUnit.Centimeter:
          type.get_Parameter(BuiltInParameter.IMPORT_DISPLAY_UNITS).Set(1);
          type.get_Parameter(BuiltInParameter.IMPORT_SCALE).Set(1.0);
          break;
        case ImportUnit.Decimeter:
          type.get_Parameter(BuiltInParameter.IMPORT_DISPLAY_UNITS).Set(1);
          type.get_Parameter(BuiltInParameter.IMPORT_SCALE).Set(10.0);
          break;
        case ImportUnit.Meter:
          type.get_Parameter(BuiltInParameter.IMPORT_DISPLAY_UNITS).Set(0);
          type.get_Parameter(BuiltInParameter.IMPORT_SCALE).Set(1.0);
          break;
        case ImportUnit.Custom:
          type.get_Parameter(BuiltInParameter.IMPORT_DISPLAY_UNITS).Set(3);
          type.get_Parameter(BuiltInParameter.IMPORT_SCALE).Set(customScale);
          break;
#if REVIT_2024
        case ImportUnit.USSurveyFoot:
          type.get_Parameter(BuiltInParameter.IMPORT_DISPLAY_UNITS).Set(3);
          type.get_Parameter(BuiltInParameter.IMPORT_SCALE).Set(1.000002000004);
          break;
#endif
      }
    }
  }
}
