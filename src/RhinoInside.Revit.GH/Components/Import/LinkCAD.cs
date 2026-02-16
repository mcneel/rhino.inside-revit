using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Collections;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Import
{
  [ComponentVersion(introduced: "1.34")]
  public class LinkCAD : LinkFileComponent
  {
    public override Guid ComponentGuid => new Guid("E2B7D9F5-3F8D-6A6E-F2C5-7E9D1B3C4E5F");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public LinkCAD() : base
    (
      name: "Link CAD",
      nickname: "LinkCAD",
      description: "Link a CAD file (DWG, DXF, DGN) to the current Revit document",
      category: "Revit",
      subCategory: "Insert"
    )
    { }

    protected override string[] SupportedExtensions => new[] { ".dwg", ".dxf", ".dgn" };
    protected override string FileFilter => "CAD Files (*.dwg, *.dxf, *.dgn)|*.dwg;*.dxf;*.dgn";

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Target document for linking the CAD file",
          Optional = true
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_FilePath
        {
          Name = "Path",
          NickName = "P",
          Description = "Absolute path to the CAD file",
          FileFilter = "CAD Files (*.dwg, *.dxf, *.dgn)|*.dwg;*.dxf;*.dgn"
        }
      ),
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = "Level",
          NickName = "L",
          Description = "Level to place the CAD link"
        }, ParamRelevance.Primary
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
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

      ReconstructElement<ARDB.ImportInstance>
      (
        doc.Value, _Output_, importInstance =>
        {
          // Input
          if (!Params.GetData(DA, "Path", out string path)) return null;
          if (!Params.GetData(DA, "Level", out Types.Level level)) return null;

          // Validate file
          ValidateFile(path);

          // Compute
          importInstance = Reconstruct(importInstance, doc.Value, path, doc.Value.ActiveView, level.Value);

          DA.SetData(_Output_, importInstance);
          return importInstance;
        }
      );
    }

    ARDB.ImportInstance Reconstruct
    (
      ARDB.ImportInstance importInstance,
      ARDB.Document doc,
      string path,
      ARDB.View view,
      ARDB.Level level
    )
    {
      if (!Reuse(importInstance, doc, path, level))
      {
        if (importInstance != null && importInstance.IsValidObject)
          doc.Delete(importInstance.Id);

        var options = new ARDB.DWGImportOptions();
        importInstance = Create(doc, path, view, options, level);
      }

      return importInstance;
    }
  }
}
