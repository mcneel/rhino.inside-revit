using System;
using System.IO;
using System.Linq;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Insert
{
  public abstract class LinkFileComponent : ElementTrackerComponent
  {
    protected LinkFileComponent(string name, string nickname, string description, string category, string subCategory)
      : base(name, nickname, description, category, subCategory)
    { }

    protected abstract string[] SupportedExtensions { get; }

    protected abstract string FileFilter { get; }

    protected void ValidateFile(string path)
    {
      if (!File.Exists(path))
        throw new Exceptions.RuntimeArgumentException("Path", $"File does not exist: {path}");

      var extension = Path.GetExtension(path).ToLowerInvariant();
      if (string.IsNullOrWhiteSpace(extension) ||
          !SupportedExtensions.Any(ext => string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase)))
      {
        var supportedList = string.Join(", ", SupportedExtensions);
        throw new Exceptions.RuntimeArgumentException(
            nameof(path),
            $"File must have one of these extensions: {supportedList}. Got: {extension ?? "none"}"
        );
      }
    }

    protected bool Reuse(ARDB.ImportInstance importInstance, ARDB.Document doc, string filePath, ARDB.Level level)
    {
      if (importInstance is null) return false;
      if (!importInstance.IsValidObject) return false;

      if (!importInstance.IsLinked) return false;

      if (!(doc.GetElement(importInstance.GetTypeId()) is ARDB.CADLinkType cadLinkType)) return false;

      var externalFileRef = cadLinkType.GetExternalFileReference();
      if (externalFileRef == null) return false;

      var linkedPath = ARDB.ModelPathUtils.ConvertModelPathToUserVisiblePath(externalFileRef.GetPath());
      if (string.IsNullOrEmpty(linkedPath)) return false;
      if (!string.Equals(linkedPath, filePath, StringComparison.OrdinalIgnoreCase))
        return false;

      if (!File.Exists(linkedPath))
        return false;

      var levelId = importInstance.get_Parameter(ARDB.BuiltInParameter.IMPORT_BASE_LEVEL).AsElementId();
      if (levelId == null || levelId == ARDB.ElementId.InvalidElementId)
        return false;

      if (levelId != level.Id)
      {
        importInstance.get_Parameter(ARDB.BuiltInParameter.IMPORT_BASE_LEVEL).Set(level.Id);
        return true;
      }

      return true;
    }

    protected ARDB.ImportInstance Create(
      ARDB.Document doc,
      string path,
      ARDB.View view,
      ARDB.BaseImportOptions options,
      ARDB.Level level)
    {
        ARDB.ElementId linkId = ARDB.ElementId.InvalidElementId;

#if REVIT_2023
        if (SupportedExtensions.Contains(".3dm") && options is ARDB.ImportOptions3DM options3dm)
              linkId = doc.Link(path, options3dm, view);
#endif
#if REVIT_2025
        if (SupportedExtensions.Contains(".obj") && options is ARDB.OBJImportOptions optionsObj)
              linkId = doc.Link(path, optionsObj, view);
#endif
        if ((SupportedExtensions.Contains(".dwg") || SupportedExtensions.Contains(".dxf")) && options is ARDB.DWGImportOptions optionsDwg)
          if (!doc.Link(path, optionsDwg, view, out linkId))
            throw new Exceptions.RuntimeException($"Failed to link file: {path}");

        if (linkId == ARDB.ElementId.InvalidElementId)
            throw new Exceptions.RuntimeException($"Unsupported file extension or import options type for: {path}");

      // Get the import instance
      if (!(doc.GetElement(linkId) is ARDB.ImportInstance importInstance))
        throw new Exceptions.RuntimeException($"Failed to get import instance for link");

      importInstance.get_Parameter(ARDB.BuiltInParameter.IMPORT_BASE_LEVEL).Set(level.Id);

      return importInstance;
    }

    protected static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties = { };
  }
}
