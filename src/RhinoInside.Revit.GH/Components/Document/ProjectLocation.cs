using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.GH.Components.Documents
{
  public class ProjectLocation : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("B8677884-61E8-4D3F-8ACB-0873B2A40053");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "⌖";

    public ProjectLocation()
    : base
    (
      "Project Location",
      "Location",
      "Project location.",
      "Revit",
      "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.Document>("Project", "P", relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Parameters.GraphicalElement>("Shared Site", "SS", "Current Shared Site", optional: true, relevance: ParamRelevance.Secondary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ElementType>("Site Location", "SL", "Project site location", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.GraphicalElement>("Shared Site", "SS", "Current Shared Site", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.GraphicalElement>("Survey Point", "SP", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.GraphicalElement>("Project Base Point", "PBP", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.GraphicalElement>("Internal Origin", "IO", relevance: ParamRelevance.Occasional),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Project", out var doc)) return;
      if (doc.IsFamilyDocument)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "'Project' is not a valid Project document");
        return;
      }

      if (Params.GetData(DA, "Shared Site", out Types.ProjectLocation location, x => x.IsValid))
      {
        if (!doc.Equals(location.Document))
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "'Shared Site' is not valid on 'Project' document");
          return;
        }

        StartTransaction(doc);
        doc.ActiveProjectLocation = location.Value;
      }

      Params.TrySetData(DA, "Site Location", () => new Types.SiteLocation(doc.SiteLocation));
      Params.TrySetData(DA, "Shared Site", () => new Types.ProjectLocation(doc.ActiveProjectLocation));
      Params.TrySetData(DA, "Survey Point", () => new Types.BasePoint(BasePointExtension.GetSurveyPoint(doc)));
      Params.TrySetData(DA, "Project Base Point", () => new Types.BasePoint(BasePointExtension.GetProjectBasePoint(doc)));
      Params.TrySetData(DA, "Internal Origin", () => new Types.InternalOrigin(InternalOriginExtension.Get(doc)));
    }
  }
}
