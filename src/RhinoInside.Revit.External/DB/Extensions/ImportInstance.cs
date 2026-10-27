using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class ImportInstanceExtension
  {
    #region Import
    public static ImportInstance Import(View view, string path, BaseImportOptions options)
    {
      var doc = view.Document;
      var instanceId = ElementId.InvalidElementId;

      if (options is DWGImportOptions optionsDwg) doc.Import(path, optionsDwg, view, out instanceId);
      else if (options is DGNImportOptions optionsDGN) doc.Import(path, optionsDGN, view, out instanceId);
      else if (options is SATImportOptions optionsSAT) instanceId = doc.Import(path, optionsSAT, view);
      else if (options is SKPImportOptions optionsSKP) instanceId = doc.Import(path, optionsSKP, view);
#if REVIT_2022
      else if (options is ImportOptions3DM options3DM) instanceId = doc.Import(path, options3DM, view);
#endif
#if REVIT_2023
      else if (options is OBJImportOptions optionsOBJ) instanceId = doc.Import(path, optionsOBJ, view);
      else if (options is STLImportOptions optionsSTL) instanceId = doc.Import(path, optionsSTL, view);
#endif
#if REVIT_2024
      else if (options is STEPImportOptions optionsSTP) instanceId = doc.Import(path, optionsSTP, view);
#endif

      return doc.GetElement(instanceId) as ImportInstance;
    }
    #endregion

    #region Link
    public static ImportInstance Link(View view, string path, BaseImportOptions options)
    {
      var doc = view.Document;
      if (options.ThisViewOnly)
      {
        var instanceId = ElementId.InvalidElementId;

        if (options is DWGImportOptions optionsDwg) doc.Link(path, optionsDwg, view, out instanceId);
        else if (options is DGNImportOptions optionsDGN) doc.Link(path, optionsDGN, view, out instanceId);
        else if (options is SATImportOptions optionsSAT) instanceId = doc.Link(path, optionsSAT, view);
        else if (options is SKPImportOptions optionsSKP) instanceId = doc.Link(path, optionsSKP, view);
#if REVIT_2022
        else if (options is ImportOptions3DM options3DM) instanceId = doc.Link(path, options3DM, view);
#endif
#if REVIT_2023
        else if (options is OBJImportOptions optionsOBJ) instanceId = doc.Link(path, optionsOBJ, view);
        else if (options is STLImportOptions optionsSTL) instanceId = doc.Link(path, optionsSTL, view);
#endif
#if REVIT_2025
        else if (options is STEPImportOptions optionsSTP) instanceId = doc.Link(path, optionsSTP, view);
#endif

        return doc.GetElement(instanceId) as ImportInstance;
      }
      else
      {
        if (CreateCADLinkResourceReference(doc, path) is ExternalResourceReference externalResource)
        {
          if (options is DWGImportOptions optionsDwg) return ImportInstance.Create(doc, view, externalResource, optionsDwg, out var result);
          if (options is DGNImportOptions optionsDGN) return ImportInstance.Create(doc, view, externalResource, optionsDGN, out var result);
          if (options is SATImportOptions optionsSAT) return ImportInstance.Create(doc, view, externalResource, optionsSAT, out var result);
          if (options is SKPImportOptions optionsSKP) return ImportInstance.Create(doc, view, externalResource, optionsSKP, out var result);
#if REVIT_2022
          if (options is ImportOptions3DM options3DM) return ImportInstance.Create(doc, view, externalResource, options3DM, out var result);
#endif
#if REVIT_2023
          if (options is OBJImportOptions optionsOBJ) return ImportInstance.Create(doc, view, externalResource, optionsOBJ, out var result);
          if (options is STLImportOptions optionsSTL) return ImportInstance.Create(doc, view, externalResource, optionsSTL, out var result); ;
#endif
#if REVIT_2025
          if (options is STEPImportOptions optionsSTP) return ImportInstance.Create(doc, view, externalResource, optionsSTP, out var result);
#endif
        }
      }

      return null;
    }

    static ExternalResourceReference CreateCADLinkResourceReference(Document doc, string absolutePath)
    {
      var modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(absolutePath);
      var resourceType = ExternalResourceTypes.BuiltInExternalResourceTypes.CADLink;
      return ExternalResourceReference.CreateLocalResource(doc, resourceType, modelPath, PathType.Absolute);
    }

    public static ProjectLocation GetSharedSite(this ImportInstance instance)
    {
      var name = instance.Name;
      if (!ElementNaming.IsValidName(name))
        return null;

      var space = name.IndexOf(' ');
      if (space != -1) name = name.Substring(space + 1);

      using (var collector = new FilteredElementCollector(instance.Document).OfClass(typeof(ProjectLocation)))
      {
        return collector.
          Cast<ProjectLocation>().
          Where(x => ElementNaming.NameEqualityComparer.Equals(x.Name, name)).
          FirstOrDefault();
      }
    }
    #endregion
  }
}
