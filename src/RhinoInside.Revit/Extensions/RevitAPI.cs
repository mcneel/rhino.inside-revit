using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit
{
  /*internal*/ public static class RevitAPI
  {
    #region Instance
    public static void SetTransform(this Instance element, XYZ newOrigin, XYZ newBasisX, XYZ newBasisY)
    {
      var current = element.GetTransform();
      var BasisZ = newBasisX.CrossProduct(newBasisY);
      {
        if (!current.BasisZ.IsParallelTo(BasisZ))
        {
          var axisDirection = current.BasisZ.CrossProduct(BasisZ);
          double angle = current.BasisZ.AngleTo(BasisZ);

          using (var axis = Line.CreateUnbound(current.Origin, axisDirection))
            ElementTransformUtils.RotateElement(element.Document, element.Id, axis, angle);

          current = element.GetTransform();
        }

        if (!current.BasisX.IsAlmostEqualTo(newBasisX))
        {
          double angle = current.BasisX.AngleOnPlaneTo(newBasisX, BasisZ);
          using (var axis = Line.CreateUnbound(current.Origin, BasisZ))
            ElementTransformUtils.RotateElement(element.Document, element.Id, axis, angle);
        }

        {
          var trans = newOrigin - current.Origin;
          if (!trans.IsZeroLength())
            ElementTransformUtils.MoveElement(element.Document, element.Id, trans);
        }
      }
    }
    #endregion

    #region ParameterFilterElement
    #endregion

    #region FilteredElementCollector
    public static FilteredElementCollector OfTypeId(this FilteredElementCollector collector, ElementId typeId)
    {
      using (var provider = new ParameterValueProvider(new ElementId(BuiltInParameter.ELEM_TYPE_PARAM)))
      using (var evaluator = new FilterNumericEquals())
      using (var rule = new FilterElementIdRule(provider, evaluator, typeId))
      using (var filter = new ElementParameterFilter(rule))
      return collector.WherePasses(filter);
    }
    #endregion

    #region Application
    public static DefinitionFile CreateSharedParameterFile(this Autodesk.Revit.ApplicationServices.Application app)
    {
      string sharedParametersFilename = app.SharedParametersFilename;
      try
      {
        // Create Temp Shared Parameters File
        app.SharedParametersFilename = Path.GetTempFileName();
        return app.OpenSharedParameterFile();
      }
      finally
      {
        // Restore User Shared Parameters File
        try { File.Delete(app.SharedParametersFilename); }
        finally { app.SharedParametersFilename = sharedParametersFilename; }
      }
    }

#if !REVIT_2018
    public static IList<Autodesk.Revit.Utility.Asset> GetAssets(this Autodesk.Revit.ApplicationServices.Application app, Autodesk.Revit.Utility.AssetType assetType)
    {
      return new Autodesk.Revit.Utility.Asset[0];
    }

    public static AppearanceAssetElement Duplicate(this AppearanceAssetElement element, string name)
    {
      return AppearanceAssetElement.Create(element.Document, name, element.GetRenderingAsset());
    }
#endif

    public static int ToLCID(this Autodesk.Revit.ApplicationServices.LanguageType value)
    {
      switch (value)
      {
        case Autodesk.Revit.ApplicationServices.LanguageType.English_USA:   return 1033;
        case Autodesk.Revit.ApplicationServices.LanguageType.German:        return 1031;
        case Autodesk.Revit.ApplicationServices.LanguageType.Spanish:       return 1034;
        case Autodesk.Revit.ApplicationServices.LanguageType.French:        return 1036;
        case Autodesk.Revit.ApplicationServices.LanguageType.Italian:       return 1040;
        case Autodesk.Revit.ApplicationServices.LanguageType.Dutch:         return 1043;
        case Autodesk.Revit.ApplicationServices.LanguageType.Chinese_Simplified: return 2052;
        case Autodesk.Revit.ApplicationServices.LanguageType.Chinese_Traditional: return 1028;
        case Autodesk.Revit.ApplicationServices.LanguageType.Japanese:      return 1041;
        case Autodesk.Revit.ApplicationServices.LanguageType.Korean:        return 1042;
        case Autodesk.Revit.ApplicationServices.LanguageType.Russian:       return 1049;
        case Autodesk.Revit.ApplicationServices.LanguageType.Czech:         return 1029;
        case Autodesk.Revit.ApplicationServices.LanguageType.Polish:        return 1045;
        case Autodesk.Revit.ApplicationServices.LanguageType.Hungarian:     return 1038;
        case Autodesk.Revit.ApplicationServices.LanguageType.Brazilian_Portuguese: return 1046;
#if REVIT_2018
        case Autodesk.Revit.ApplicationServices.LanguageType.English_GB: return 2057;
#endif
      }

      return 1033;
    }
    #endregion
  }
}
