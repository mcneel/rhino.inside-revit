using System;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Insert
{
  [ComponentVersion(introduced: "1.36")]
  public class LinkRevit : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("F3C8E0A6-4E9E-7A7F-E3C6-8A0E2C4D5F6E");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => string.Empty;

    public LinkRevit() : base
    (
      name: "Link Revit File",
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
          FileFilter = "Revit Files (*.rvt)|*.rvt"
        }
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
          Description = $"Output {_Output_}"
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

          // Validations
          if (!File.Exists(path))
            throw new Exceptions.RuntimeArgumentException("Path", $"Revit file does not exist: {path}");

          var extension = Path.GetExtension(path).ToLowerInvariant();
          if (extension != ".rvt")
            throw new Exceptions.RuntimeArgumentException("Path", $"File must be a Revit file (*.rvt): {path}");
          
          // Compute
          linkInstance = Reconstruct(linkInstance, doc.Value, path);

          DA.SetData(_Output_, linkInstance);
          return linkInstance;
        }
      );
    }

    bool Reuse(ref ARDB.RevitLinkInstance linkInstance, string rvtPath)
    {
      if (linkInstance is null) return false;

      if (!(linkInstance.Document.GetElement(linkInstance.GetTypeId()) is ARDB.RevitLinkType linkType)) return false;

      var externalFileRef = linkType.GetExternalFileReference();
      if (externalFileRef == null) return false;

      var linkedPath = ARDB.ModelPathUtils.ConvertModelPathToUserVisiblePath(externalFileRef.GetPath());
      if (string.IsNullOrEmpty(linkedPath)) return false;

      if (!PathExtension.Comparer.Equals(linkedPath, rvtPath))
      {
        var typeId = GetLinkTypeId(linkInstance.Document, rvtPath);
        if (!typeId.IsValid())
          return false;

        if (ARDB.Element.IsValidType(linkInstance.Document, new ARDB.ElementId[] { linkInstance.Id }, typeId))
        {
          if (linkInstance.ChangeTypeId(typeId) is ARDB.ElementId id && id != ARDB.ElementId.InvalidElementId)
            linkInstance = linkInstance.Document.GetElement(id) as ARDB.RevitLinkInstance;
        }
        else return false;
      }

      return true;
    }

    ARDB.ElementId CreateLinkType(ARDB.Document doc, string path)
    {
      var typeId = GetLinkTypeId(doc, path);
      if (typeId.IsValid()) return typeId;

      var linkOptions = new ARDB.RevitLinkOptions(relative:false);
      var modelPath = ARDB.ModelPathUtils.ConvertUserVisiblePathToModelPath(path);
      var linkLoadResult = ARDB.RevitLinkType.Create(doc, modelPath, linkOptions);

      if (linkLoadResult.ElementId == ARDB.ElementId.InvalidElementId)
        throw new Exceptions.RuntimeException($"Failed to create Revit link from file: {path}");

      return linkLoadResult.ElementId;
    }

    public ARDB.ElementId GetLinkTypeId(ARDB.Document doc, string rvtPath)
    {
      using (var linkTypes = new ARDB.FilteredElementCollector(doc).OfClass(typeof(ARDB.RevitLinkType)))
      {
        foreach (var linkType in linkTypes.Cast<ARDB.RevitLinkType>())
        {
          if (linkType.IsNestedLink)
            continue;

          if (!linkType.IsFromLocalPath())
            continue;

          var extRef = linkType.GetExternalFileReference();
          if (extRef == null)
            continue;

          var currentLinkPath = ARDB.ModelPathUtils.ConvertModelPathToUserVisiblePath(extRef.GetAbsolutePath());
          if (PathExtension.Comparer.Equals(currentLinkPath, rvtPath))
            return linkType.Id;
        }
      }

      return ElementIdExtension.Invalid;
    }

    ARDB.RevitLinkInstance Reconstruct
    (
      ARDB.RevitLinkInstance linkInstance,
      ARDB.Document doc,
      string path
    )
    {
      if (!Reuse(ref linkInstance, path))
        linkInstance = ARDB.RevitLinkInstance.Create(doc, CreateLinkType(doc, path));

      return linkInstance;
    }
  }
}
