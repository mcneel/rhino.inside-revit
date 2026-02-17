using System;
using System.IO;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Insert
{
  [ComponentVersion(introduced: "1.36")]
  public class LinkRevit : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("F3C8E0A6-4E9E-7A7F-E3C6-8A0E2C4D5F6E");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public LinkRevit() : base
    (
      name: "Link Revit",
      nickname: "LinkRVT",
      description: "Link a Revit file (RVT, RFA) to the current Revit document",
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
          Description = "Target document for linking the Revit file",
          Optional = true
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_FilePath
        {
          Name = "Path",
          NickName = "P",
          Description = "Absolute path to the Revit file",
          FileFilter = "Revit Files (*.rvt, *.rfa)|*.rvt;*.rfa"
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Absolute",
          NickName = "A",
          Description = "Use absolute path (true) or relative path (false)",
          Optional = true
        }, ParamRelevance.Secondary
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
          Params.TryGetData(DA, "Absolute", out bool? absolute);

          // Validations
          if (!File.Exists(path))
            throw new Exceptions.RuntimeArgumentException("Path", $"Revit file does not exist: {path}");

          var extension = Path.GetExtension(path).ToLowerInvariant();
          if (extension != ".rvt" && extension != ".rfa")
            throw new Exceptions.RuntimeArgumentException("Path", $"File must be a Revit file (.rvt or .rfa): {path}");
          
          // Compute
          linkInstance = Reconstruct(linkInstance, doc.Value, path, absolute ?? false);

          DA.SetData(_Output_, linkInstance);
          return linkInstance;
        }
      );
    }

    bool Reuse(ARDB.RevitLinkInstance linkInstance, ARDB.Document doc, string rvtPath)
    {
      if (linkInstance is null) return false;
      if (!linkInstance.IsValidObject) return false;

      if (!(doc.GetElement(linkInstance.GetTypeId()) is ARDB.RevitLinkType linkType)) return false;

      var externalFileRef = linkType.GetExternalFileReference();
      if (externalFileRef == null) return false;

      var linkedPath = ARDB.ModelPathUtils.ConvertModelPathToUserVisiblePath(externalFileRef.GetPath());
      if (string.IsNullOrEmpty(linkedPath)) return false;

      var normalizedLinkedPath = Path.GetFullPath(linkedPath);
      var normalizedInputPath = Path.GetFullPath(rvtPath);

      if (!string.Equals(normalizedLinkedPath, normalizedInputPath, StringComparison.OrdinalIgnoreCase))
        return false;

      if (!File.Exists(linkedPath))
        return false;

      var rvtModifiedTime = File.GetLastWriteTimeUtc(rvtPath);
      var linkStatus = linkType.GetLinkedFileStatus();

      if (linkStatus != ARDB.LinkedFileStatus.Loaded)
        return false;

      return true;
    }

    ARDB.RevitLinkInstance Create(ARDB.Document doc, string path, bool absolute)
    {
      var modelPath = ARDB.ModelPathUtils.ConvertUserVisiblePathToModelPath(path);

      var linkOptions = new ARDB.RevitLinkOptions(!absolute);
      var linkLoadResult = ARDB.RevitLinkType.Create(doc, modelPath, linkOptions);

      if (linkLoadResult.ElementId == ARDB.ElementId.InvalidElementId)
        throw new Exceptions.RuntimeException($"Failed to create Revit link from file: {path}");

      var linkInstance = ARDB.RevitLinkInstance.Create(doc, linkLoadResult.ElementId);

      if (linkInstance == null)
        throw new Exceptions.RuntimeException($"Failed to create link instance for Revit link");

      return linkInstance;
    }

    ARDB.RevitLinkInstance Reconstruct
    (
      ARDB.RevitLinkInstance linkInstance,
      ARDB.Document doc,
      string path,
      bool absolute
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

        linkInstance = Create(doc, path, absolute);
      }

      return linkInstance;
    }
  }
}
