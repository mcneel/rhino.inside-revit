using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.Documents
{
  [ComponentVersion(introduced: "1.6")]
  public class DocumentVersion : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("8A2DA785-098B-466F-B715-FEA46070EFCF");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override string IconTag => string.Empty;

    public DocumentVersion() : base
    (
      name: "Document Version",
      nickname: "Version",
      description: "Document version information",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_Boolean>("Edited", "E", "Identifies if the document has been modified since it was opened", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Boolean>("Editable", "EB", "Identifies if the document is read-only or can possibly be modified", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Guid>("Created", "C", "Document episode when it was created", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Guid>("Version", "V", "Document episode when it was edited last time", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Integer>("Saves", "S", "The number of times the document has been saved", relevance: ParamRelevance.Primary),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;
      else Params.TrySetData(DA, "Document", () => doc);

      Params.TrySetData(DA, "Edited", () => doc.IsModified);
      Params.TrySetData(DA, "Editable", () => doc.IsEditable);
      Params.TrySetData(DA, "Created", () => doc.ExportID);

      var version = doc.Version;
      if (version.HasValue)
      {
        Params.TrySetData(DA, "Version", () => version.Value.VersionGUID);
        Params.TrySetData(DA, "Saves", () => version.Value.NumberOfSaves);
      }
    }
  }
}
