using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Documents
{
  [ComponentVersion(introduced: "1.36")]
  public class DocumentLoad : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("CA8912CE-19A9-404C-95EE-C758A5C7E4C9");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public DocumentLoad() : base
    (
      name: "Load Document",
      nickname: "D-Load",
      description: "Loads a file in the background as a Revit document.",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Param_FilePath
        {
          Name = "Path",
          NickName = "P",
          Description = "Absolute path to the file",
          FileFilter = "Files (*.rte, *.rvt, *.rfa, *.rvf, *.ifc, *.ifczip)|*.rte;*.rvt;*.rfa;*.rvf;*.ifc;*.ifczip"
        }
      ),
      new ParamDefinition
      (
        new Param_GenericObject
        {
          Name = "Options",
          NickName = "O",
          Description = "Load options",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Load",
          NickName = "L",
          Description = "Load",
        }.SetDefaultVale(false), ParamRelevance.Secondary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = _Output_,
          NickName = _Output_.Substring(0, 1),
          Description = $"Output {_Output_}",
          Access = GH_ParamAccess.item
        }
      )
    };

    const string _Output_ = "Document";

    static readonly ISet<string> FileExtensions = new HashSet<string>(PathExtension.Comparer){ ".rte", ".rvt", ".rfa", ".ifc", ".ifczip" };
    Dictionary<string, Types.Document> _cachedDocs = new Dictionary<string, Types.Document>(PathExtension.Comparer);
    Dictionary<string, Types.Document> _currentDocs = new Dictionary<string, Types.Document>(PathExtension.Comparer);

    void PurgeCachedDocs()
    {
      try
      {
        Guest.Instance.CommitTransactionGroups();

        foreach (var tempDoc in _cachedDocs.Values)
          tempDoc.Value?.Release();
      }
      finally
      {
        Guest.Instance.StartTransactionGroups();
      }

      _cachedDocs.Clear();
    }

    public override void RemovedFromDocument(GH_Document document)
    {
      base.RemovedFromDocument(document);
      PurgeCachedDocs();
    }

    protected override void BeforeSolveInstance()
    {
      _currentDocs = new Dictionary<string, Types.Document>();
      base.BeforeSolveInstance();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      int _in_ = 0;
      if (!Params.TryGetData(DA, inputs[_in_++].Param.Name, out string path)) return;
      if (!Params.TryGetData(DA, inputs[_in_++].Param.Name, out ARDB.IFC.IFCImportOptions options)) return;
      if (!Params.TryGetData(DA, inputs[_in_++].Param.Name, out bool? load)) return;

      // Validations
      var extension = Path.GetExtension(path);
      if (!FileExtensions.Contains(extension))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"'{extension.TripleDot(10)}' is not a supported extension.");
        return;
      }

      if (!PathExtension.TryGetFullyQualifiedPath(ref path))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"'{path.TripleDotPath(256)}' is not a valid absolute file path.");
        return;
      }

      load = load ?? ((Attributes as CustomAttributes).Pressed ? true : default(bool?));
      if (TryGetDocument(path, load, out var document, options))
        DA.SetData(_Output_, document);
    }

    protected override void AfterSolveInstance()
    {
      base.AfterSolveInstance();

      PurgeCachedDocs();
      _cachedDocs = _currentDocs;
      _currentDocs = null;
    }

    private bool TryGetDocument(string uri, bool? import, out Types.Document document, ARDB.IFC.IFCImportOptions options)
    {
      if (_currentDocs.TryGetValue(uri, out document))
        return document is object;

      if (!File.Exists(uri))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"File '{uri.TripleDotPath(64)}' does not exist.");
        _currentDocs[uri] = document = null;
        return false;
      }

      if (_cachedDocs.TryGetValue(uri, out document))
      {
        _cachedDocs.Remove(uri);

#if DEBUG
        if (File.GetLastWriteTimeUtc(uri) == document.File.LastWriteTimeUtc)
        {
          _currentDocs[uri] = document;
          return true;
        }
#endif

        if (import != true)
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"File '{uri}' has changed since last time it was imported.");

        _currentDocs[uri] = document;

        if (!import.HasValue)
          return true;
      }
      else if (!import.HasValue)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"File '{uri}' is not imported.{System.Environment.NewLine}Use the 'Import' button to effectively import");
      }

      if (import is true)
      {
        try
        {
          document = Types.Document.FromValue(LoadDocument(uri, options));
          if (document is object)
          {
            _currentDocs[uri] = document;
            return true;
          }

          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to read file {Path.GetFileName(uri)}");
        }
        catch (Exception e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Error during file reading: " + e.Message);
        }
      }

      document = null;
      return false;
    }

    ARDB.Document LoadDocument(string path, ARDB.IFC.IFCImportOptions options)
    {
      if (string.Equals(Path.GetExtension(path), ".rvt", StringComparison.OrdinalIgnoreCase))
      {
        using (var optionsRVT = new ARDB.OpenOptions())
          return Revit.ActiveDBApplication.OpenDocumentFile(new ARDB.FilePath(path), optionsRVT);
      }

      if (options == null)
        options = new ARDB.IFC.IFCImportOptions();

      if (options.Action != ARDB.IFC.IFCImportAction.Open)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
          $"IFC Import Action is set to '{options.Action}'. OpenIFCDocument only supports 'Open' action. " +
          "Setting Action to Open.");
        options.Action = ARDB.IFC.IFCImportAction.Open;
      }

      if (options.Intent != ARDB.IFC.IFCImportIntent.Parametric)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
          $"IFC Import Intent is set to '{options.Intent}'. OpenIFCDocument works best with 'Parametric' intent. " +
          "Setting Intent to Parametric.");
        options.Intent = ARDB.IFC.IFCImportIntent.Parametric;

        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
          "For Link action or Reference intent, use the 'Link IFC' component instead.");
      }

      var app = Revit.ActiveUIApplication.Application;

      var ifcDocument = app.OpenIFCDocument(path, options);

      if (ifcDocument == null || !ifcDocument.IsValidObject)
        throw new Exceptions.RuntimeException($"Failed to open IFC file: {path}");

      return ifcDocument;
    }

    #region UI
    public override void CreateAttributes() => m_attributes = new CustomAttributes(this);

    class CustomAttributes : ExpireButtonAttributes
    {
      public CustomAttributes(ZuiComponent owner) : base(owner) { }

      protected override string DisplayText => "Load";
      protected override bool Visible => Owner.Params.IndexOfInputParam("Load") < 0;
    }
    #endregion
  }
}
