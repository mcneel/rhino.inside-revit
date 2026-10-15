using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  using Grasshopper.Kernel.Parameters;

  [ComponentVersion(introduced: "1.37")]
  public class ElementInterferences : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("6F9AB7AF-302C-478B-BD61-EB989ACBE630");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public ElementInterferences() : base
    (
      name: "Check Element Interferences",
      nickname: "E-Interferences",
      description: "Check for solid-solid interferences betwen elements.",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.GraphicalElement>("Element", "E", "Target element"),
      ParamDefinition.Create<Param_Boolean>("Voids", "V", "Check also family voids", defaultValue: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Boolean>("Phasing", "P", "Check that elements also coincide on time", defaultValue: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.GraphicalElement>("Candidates", "C", "Candidate elements for interference checking", access: GH_ParamAccess.list),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Target element",
          DataMapping = GH_DataMapping.Graft
        },ParamRelevance.Primary
      ),
      ParamDefinition.Create<Parameters.GraphicalElement>("Interferences", "I", "Elements interferences list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.GraphicalElement element)) return;
      Params.TrySetData(DA, "Element", () => element);
      if (!Params.TryGetData(DA, "Voids", out bool? voids)) return;
      if (!Params.TryGetData(DA, "Phasing", out bool? phasing)) return;
      if (!Params.TryGetDataList(DA, "Candidates", out IList<Types.GraphicalElement> candidates)) return;

      DA.SetDataList
      (
        "Interferences", element.GetSolidInterferences
        (
          candidates.Where(x => x?.IsValid is true),
          voids ?? false,
          phasing ?? false
        ).
        TakeWhileIsNotEscapeKeyDown(this)
      );
    }
  }
}
