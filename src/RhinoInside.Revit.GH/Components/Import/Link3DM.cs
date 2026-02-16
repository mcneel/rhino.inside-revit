using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Import
{
  [ComponentVersion(introduced: "1.36"), ComponentRevitAPIVersion(min: "2020.0")]
  public class Link3DM : LinkFileComponent
  {
    public override Guid ComponentGuid => new Guid("A4D8F1B6-5E9F-7B8E-D4A7-9C1F3D5E6A7B");
    public override GH_Exposure Exposure => SDKCompliancy(GH_Exposure.tertiary);
    protected override string IconTag => string.Empty;

    public Link3DM() : base
    (
      name: "Link 3DM",
      nickname: "Link3DM",
      description: "Link a Rhino 3DM file to the current Revit document",
      category: "Revit",
      subCategory: "Insert"
    )
    { }

    protected override string[] SupportedExtensions => new[] { ".3dm" };
    protected override string FileFilter => "Rhino 3D Model (*.3dm)|*.3dm";

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Target document for linking the 3DM file",
          Optional = true
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_FilePath
        {
          Name = "Path",
          NickName = "P",
          Description = "Absolute path to the 3DM file",
          FileFilter = "Rhino 3D Model (*.3dm)|*.3dm"
        }
      ),
      new ParamDefinition
      (
        new Parameters.Level()
        {
          Name = "Level",
          NickName = "L",
          Description = "Level to place the 3DM link"
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
#if REVIT_2022
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
#endif
    }

#if REVIT_2022
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

        var options = new ARDB.ImportOptions3DM();
        importInstance = Create(doc, path, view, options, level);
      }

      return importInstance;
    }
#endif
  }
}
