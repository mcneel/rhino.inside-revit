using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Document
{
  [ComponentVersion(introduced: "1.6")]
  public class DocumentVersion : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("8A2DA785-098B-466F-B715-FEA46070EFCF");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public DocumentVersion() : base
    (
      name: "Document Version",
      nickname: "Version",
      description: string.Empty,
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
      ParamDefinition.Create<Param_Boolean>("Modifiable", "M", "Identifies if the document is read-only or can possibly be modified", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Boolean>("Modified", "MD", "Identifies if the document has been modified", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Guid>("Version", "V", "Document episode when it was last saved", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Integer>("Number Of Saves", "N", "The number of times the document has been saved", relevance: ParamRelevance.Primary),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;
      else Params.TrySetData(DA, "Document", () => doc);

      Params.TrySetData(DA, "Modifiable", () => doc.IsReadOnly == false);
      Params.TrySetData(DA, "Modified", () => doc.IsModified);

      var version = doc.Version;
      if (version.HasValue)
      {
        Params.TrySetData(DA, "Version", () => version.Value.VersionGUID);
        Params.TrySetData(DA, "Number Of Saves", () => version.Value.NumberOfSaves);
      }
    }
  }
}
