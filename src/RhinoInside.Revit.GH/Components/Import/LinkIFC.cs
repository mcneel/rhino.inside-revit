using System;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Insert
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.36")]
  public class LinkIFC : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("D1A6C8F4-2E7B-5D5C-E1A4-6F8C0A2B3C4D");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public LinkIFC() : base
    (
      name: "Link IFC File",
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

    bool Reuse(ref ARDB.RevitLinkInstance linkInstance, string ifcPath)
    {
      var rvtPath = ifcPath + ".RVT";
      if (linkInstance is null) return false;

      if (!(linkInstance.Document.GetElement(linkInstance.GetTypeId()) is ARDB.RevitLinkType linkType)) return false;

      var externalFileRef = linkType.GetExternalFileReference();
      if (externalFileRef == null) return false;

      var linkedPath = ARDB.ModelPathUtils.ConvertModelPathToUserVisiblePath(externalFileRef.GetPath());
      if (string.IsNullOrEmpty(linkedPath)) return false;

      var normalizedLinkedPath = Path.GetFullPath(linkedPath);
      var normalizedInputPath = Path.GetFullPath(rvtPath);

      if (!string.Equals(normalizedLinkedPath, normalizedInputPath, StringComparison.OrdinalIgnoreCase))
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

      if (File.GetLastWriteTimeUtc(ifcPath) > File.GetLastWriteTimeUtc(rvtPath))
        return false;

      return true;
    }

    ARDB.ElementId CreateLinkType(ARDB.Document doc, string ifcPath, ARDB.IFC.IFCImportOptions options)
    {
      var rvtPath = ifcPath + ".RVT";
      var typeId = GetLinkTypeId(doc, rvtPath);
      if (typeId.IsValid()) return typeId;

      if (File.Exists(rvtPath))
      {
        var ifcModifiedTime = File.GetLastWriteTimeUtc(ifcPath);
        var rvtModifiedTime = File.GetLastWriteTimeUtc(rvtPath);

        if (ifcModifiedTime > rvtModifiedTime)
          File.Delete(rvtPath);
      }

      if (!File.Exists(rvtPath))
      {
        try
        {
          using (var ifcDocument = doc.Application.OpenIFCDocument(ifcPath, options ?? new ARDB.IFC.IFCImportOptions()))
          {
            try { ifcDocument.SaveAs(rvtPath); }
            catch (Exception ex)
            {
              if (File.Exists(rvtPath))
                File.Delete(rvtPath);

              throw new Exceptions.RuntimeException($"Error creating IFC link: {ex.Message}", ex);
            }
            finally { ifcDocument.Close(false); }
          }
        }
        catch
        {
          throw new Exceptions.RuntimeException($"Failed to open IFC file: {ifcPath}");
        }
      }

      var linkOptions = new ARDB.RevitLinkOptions(false);
      var linkLoadResult = ARDB.RevitLinkType.CreateFromIFC(doc, ifcPath, rvtPath, false, linkOptions);
      if (linkLoadResult.ElementId == ARDB.ElementId.InvalidElementId)
        throw new Exceptions.RuntimeException($"Failed to create link from IFC file: {ifcPath}");

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
          if (string.Equals(currentLinkPath, rvtPath, StringComparison.OrdinalIgnoreCase))
            return linkType.Id;
        }
      }

      return ElementIdExtension.Invalid;
    }

    ARDB.RevitLinkInstance Reconstruct
    (
      ARDB.RevitLinkInstance linkInstance,
      ARDB.Document doc,
      string path,
      ARDB.IFC.IFCImportOptions options
    )
    {
      if (!Reuse(ref linkInstance, path))
        linkInstance = ARDB.RevitLinkInstance.Create(doc, CreateLinkType(doc, path, options));

      return linkInstance;
    }
  }
}
