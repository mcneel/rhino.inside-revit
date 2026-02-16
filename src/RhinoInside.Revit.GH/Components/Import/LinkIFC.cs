using System;
using System.IO;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Import
{
  [ComponentVersion(introduced: "1.36")]
  public class LinkIFC : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("D1A6C8F4-2E7B-5D5C-E1A4-6F8C0A2B3C4D");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public LinkIFC() : base
    (
      name: "Link IFC",
      nickname: "LinkIFC",
      description: "Link an IFC file to the current Revit document",
      category: "Revit",
      subCategory: "Insert"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Target document for linking the IFC file",
          Optional = true
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_FilePath
        {
          Name = "Path",
          NickName = "P",
          Description = "Absolute path to the IFC file",
          FileFilter = "IFC Files (*.ifc, *.ifczip)|*.ifc;*.ifczip"
        }
      ),
      new ParamDefinition
      (
        new Param_GenericObject
        {
          Name = "Options",
          NickName = "O",
          Description = "IFC import options (optional, controls how IFC is converted to RVT before linking)",
          Optional = true
        }, ParamRelevance.Primary
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _Output_,
          NickName = _Output_.Substring(0, 1),
          Description = $"Output {_Output_}",
          Access = GH_ParamAccess.item
        }
      )
    };

    const string _Output_ = "Link";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.RevitLinkInstance>
      (
        doc.Value, _Output_, linkInstance =>
        {
          // Input
          if (!Params.GetData(DA, "Path", out string path)) return null;
          Params.TryGetData(DA, "Options", out ARDB.IFC.IFCImportOptions options);

          // Validations
          if (!File.Exists(path))
            throw new Exceptions.RuntimeArgumentException("Path", $"IFC file does not exist: {path}");

          var extension = Path.GetExtension(path).ToLowerInvariant();
          if (extension != ".ifc" && extension != ".ifczip")
            throw new Exceptions.RuntimeArgumentException("Path", $"File must be an IFC file (.ifc or .ifczip): {path}");

          // Compute
          linkInstance = Reconstruct(linkInstance, doc.Value, path, options);

          DA.SetData(_Output_, linkInstance);
          return linkInstance;
        }
      );
    }

    bool Reuse(ARDB.RevitLinkInstance linkInstance, ARDB.Document doc, string ifcPath)
    {
      if (linkInstance is null) return false;

      // Check if the link instance is still valid
      if (!linkInstance.IsValidObject) return false;

      // Get the link type
      if (!(doc.GetElement(linkInstance.GetTypeId()) is ARDB.RevitLinkType linkType)) return false;

      // Get the external file reference to access the linked file path
      var externalFileRef = linkType.GetExternalFileReference();
      if (externalFileRef == null) return false;

      // Get the path to the linked RVT file
      var linkedRvtPath = ARDB.ModelPathUtils.ConvertModelPathToUserVisiblePath(externalFileRef.GetPath());
      if (string.IsNullOrEmpty(linkedRvtPath)) return false;

      // Verify the linked RVT file still exists
      if (!File.Exists(linkedRvtPath))
        return false;

      // Strategy: Store the original IFC path in the link type's parameter or check by timestamp
      // Since we can't easily get the original IFC path, we'll compare by:
      // 1. File name (without extension)
      // 2. File modification time (if the IFC file changed, we should recreate)

      var ifcFileName = Path.GetFileNameWithoutExtension(ifcPath);

      // Check if the link name contains the IFC file name
      if (!linkType.Name.Contains(ifcFileName))
        return false;

      // Get the modification time of the current IFC file
      var ifcModifiedTime = File.GetLastWriteTimeUtc(ifcPath);

      // Get the creation time of the linked RVT file (when we converted it)
      var rvtCreationTime = File.GetCreationTimeUtc(linkedRvtPath);

      // If the IFC file was modified after we created the RVT link, we need to recreate
      if (ifcModifiedTime > rvtCreationTime)
        return false;

      // If we reach here, the link can be reused
      return true;
    }

    ARDB.RevitLinkInstance Create(ARDB.Document doc, string path, ARDB.IFC.IFCImportOptions options)
    {
      var app = Revit.ActiveUIApplication.Application;

      // Generate a unique temporary path for the RVT file
      var tempFileName = $"{Path.GetFileNameWithoutExtension(path)}_IFC_{Guid.NewGuid():N}.rvt";
      var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);

      ARDB.Document tempDoc = null;

      try
      {
        if (File.Exists(tempPath))
          File.Delete(tempPath);

        // Create default options if not provided
        if (options == null)
          options = new ARDB.IFC.IFCImportOptions();

        // Convert IFC to RVT using OpenIFCDocument
        tempDoc = app.OpenIFCDocument(path, options);

        if (tempDoc == null || !tempDoc.IsValidObject)
          throw new Exceptions.RuntimeException($"Failed to open IFC file: {path}");

        tempDoc.SaveAs(tempPath);
        tempDoc.Close(false);

        // Create RevitLinkType
        var linkOptions = new ARDB.RevitLinkOptions(false);
        var linkLoadResult = ARDB.RevitLinkType.CreateFromIFC(doc, path, tempPath, false, linkOptions);

        if (linkLoadResult.ElementId == ARDB.ElementId.InvalidElementId)
          throw new Exceptions.RuntimeException($"Failed to create link from IFC file: {path}");

        // Create link instance
        var linkInstance = ARDB.RevitLinkInstance.Create(doc, linkLoadResult.ElementId);

        if (linkInstance == null)
          throw new Exceptions.RuntimeException($"Failed to create link instance from link type");

        return linkInstance;
      }
      catch (Exception ex)
      {
        if (tempDoc != null && tempDoc.IsValidObject)
          tempDoc.Close(false);

        if (File.Exists(tempPath))
          File.Delete(tempPath);

        throw new Exceptions.RuntimeException($"Error creating IFC link: {ex.Message}");
      }
    }

    ARDB.RevitLinkInstance Reconstruct
    (
      ARDB.RevitLinkInstance linkInstance,
      ARDB.Document doc,
      string path,
      ARDB.IFC.IFCImportOptions options
    )
    {
      if (!Reuse(linkInstance, doc, path))
      {
        if (linkInstance != null && linkInstance.IsValidObject)
        {
          var linkTypeId = linkInstance.GetTypeId();
          doc.Delete(linkInstance.Id);

          if (linkTypeId != ARDB.ElementId.InvalidElementId)
          {
            if (doc.GetElement(linkTypeId) is ARDB.RevitLinkType linkType && linkType.IsValidObject)
            {
              doc.Delete(linkTypeId);
            }
          }
        }

        linkInstance = Create(doc, path, options);
      }

      return linkInstance;
    }
  }
}
