using System;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  public class ElementMaterials : Component
  {
    public override Guid ComponentGuid => new Guid("93C18DFD-FAAB-4CF1-A681-C11754C2495D");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "M";

    public ElementMaterials()
    : base
    (
     name: "Element Materials",
     nickname: "Materials",
     description: "Query element used materials",
     category: "Revit",
     subCategory: "Element"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Element", "E", "Element to query for its materials", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Material(), "Materials", "M", "Materials this Element is made of", GH_ParamAccess.list);
      manager.AddParameter(new Parameters.Material(), "Paint", "P", "Materials used to paint this Element", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      Types.Element element = null;
      if (!DA.GetData("Element", ref element) || !element.IsValid)
        return;

      DA.SetDataList("Materials", element.Value.GetMaterialIds(false).Select(x => Types.Material.FromElementId(element.Document, x)));
      DA.SetDataList("Paint",     element.Value.GetMaterialIds(true ).Select(x => Types.Material.FromElementId(element.Document, x)));
    }
  }

  [ComponentVersion(introduced: "1.10")]
  public class ElementFacePaintMaterial : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("2A4A95D5-3DBD-4056-9C76-C51E6411CDB5");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public ElementFacePaintMaterial()
    : base
    (
      "Element Face Paint",
      "FacePaint",
      "Get-Set access component to material used to paint an Element Face.",
      "Revit",
      "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.GeometryFace>("Face", "F", "Face to query about its paint material."),
      ParamDefinition.Create<Parameters.Material>("Paint", "P", "Material used to paint Face", optional: true, relevance: ParamRelevance.Primary)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.GeometryFace>("Face", "F", "Face queried about its paint material."),
      ParamDefinition.Create<Parameters.Material>("Paint", "P", "Material used to paint Face", relevance: ParamRelevance.Primary)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (Params.TryGetData(DA, "Face", out Types.GeometryFace face, x => x.IsValid))
        Params.TrySetData(DA, "Face", () => face);

      if (Params.GetData(DA, "Paint", out Types.Material paint))
      {
        StartTransaction(face.Document);
        if (paint.Id == ARDB.ElementId.InvalidElementId)
          face.Document.RemovePaint(face.Id, face.Value);
        else
          face.Document.Paint(face.Id, face.Value, paint.Id);
      }

      Params.TrySetData(DA, "Paint", () => new Types.Material(face.Document, face.Document.GetPaintedMaterial(face.Id, face.Value)));
    }
  }
}
