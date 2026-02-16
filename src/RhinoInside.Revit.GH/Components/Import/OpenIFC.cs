using System;
using System.IO;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Import
{
  [ComponentVersion(introduced: "1.34")]
  public class OpenIFC : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("CA8912CE-19A9-404C-95EE-C758A5C7E4C9");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public OpenIFC() : base
    (
      name: "Open IFC",
      nickname: "OpenIFC",
      description: "Open an IFC file as a new Revit document (does not link to current document)",
      category: "Revit",
      subCategory: "Insert"
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
          Description = "IFC import options (optional, Action will be forced to 'Open')",
          Optional = true
        }, ParamRelevance.Primary
      )
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

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Params.GetData(DA, "Path", out string path)) return;
      Params.TryGetData(DA, "Options", out ARDB.IFC.IFCImportOptions options);

      // Validations
      if (!File.Exists(path))
        throw new Exceptions.RuntimeArgumentException("Path", $"IFC file does not exist: {path}");

      var extension = Path.GetExtension(path).ToLowerInvariant();
      if (extension != ".ifc" && extension != ".ifczip")
        throw new Exceptions.RuntimeArgumentException("Path", $"File must be an IFC file (.ifc or .ifczip): {path}");

      // Compute
      var ifcDocument = OpenIFCDocument(path, options);

      DA.SetData(_Output_, ifcDocument);
    }

    ARDB.Document OpenIFCDocument(string path, ARDB.IFC.IFCImportOptions options)
    {
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
  }
}
