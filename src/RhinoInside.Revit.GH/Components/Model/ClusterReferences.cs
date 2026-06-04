using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Model
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.36")]
  public class ClusterReferences : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("16889F14-BC45-4A91-98B3-999AFB0C30D8");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => string.Empty;

    public ClusterReferences() : base
    (
      name: "Cluster References",
      nickname: "C-References",
      description: "Split a list of references into separate clusters by their type",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.GeometryObject>("References", "R", "Reference list", GH_ParamAccess.list),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.GeometryObject>("Elements", "E", "Element references", GH_ParamAccess.list, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.GeometryObject>("Subelements", "S", "Subelement references", GH_ParamAccess.list, relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Parameters.GeometryPoint>("Points", "X", "Point references", GH_ParamAccess.list, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.GeometryCurve>("Curves", "C", "Curve references", GH_ParamAccess.list, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.GeometryFace>("Planes", "P", "Plane references", GH_ParamAccess.list, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.GeometryCurve>("Edges", "E", "Edge references", GH_ParamAccess.list, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.GeometryFace>("Faces", "F", "Face references", GH_ParamAccess.list, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.GeometryFace>("Meshes", "M", "Mesh references", GH_ParamAccess.list, relevance: ParamRelevance.Primary),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (Params.GetDataList(DA, "References", out IList<Types.GeometryObject> list))
      {
        var references = list.TakeWhileIsNotEscapeKeyDown(this);

        Params.TrySetDataList(DA, "Elements", () => references.OfType<Types.GeometryElement>());
        Params.TrySetDataList(DA, "Subelements", () => references.OfType<Types.GeometrySubelement>());
        Params.TrySetDataList(DA, "Points", () => references.OfType<Types.GeometryPoint>());
        Params.TrySetDataList(DA, "Curves", () => references.OfType<Types.GeometryCurve>().Where(x => x.GetReference().IsKindOf<ARDB.Curve>(x.ReferenceDocument)));
        Params.TrySetDataList(DA, "Planes", () => references.OfType<Types.GeometryFace>().Where(x => x.Value is null));
        Params.TrySetDataList(DA, "Edges", () => references.OfType<Types.GeometryCurve>().Where(x => x.GetReference().IsKindOf<ARDB.Edge>(x.ReferenceDocument)));
        Params.TrySetDataList(DA, "Faces", () => references.OfType<Types.GeometryFace>().Where(x => x.Value is object));
        Params.TrySetDataList(DA, "Meshes", () => references.OfType<Types.GeometryMesh>());
      }
    }
  }
}
