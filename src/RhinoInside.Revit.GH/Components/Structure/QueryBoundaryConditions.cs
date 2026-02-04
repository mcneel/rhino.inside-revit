using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
  [ComponentVersion(introduced: "1.27", updated: "1.36")]
  public class QueryBoundaryConditions : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("F8C6588F-4131-4692-A43D-CC4E34F88AB4");
    public override GH_Exposure Exposure => GH_Exposure.senary | GH_Exposure.obscure;

    private ARDB.ElementFilter _ElementFilter = new ARDB.ElementClassFilter(typeof(ARDB.Structure.BoundaryConditions));
    protected override ARDB.ElementFilter ElementFilter => _ElementFilter;

    public QueryBoundaryConditions() : base
    (
      name: "Query Boundary Conditions",
      nickname: "BC",
      description: "Get all document boundary conditions",
      category: "Revit",
      subCategory: "Structure"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.ModelInstance(), ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.BoundaryConditionsType>
        {
          Name = "Type",
          NickName = "T",
          Description = "Boundary Conditions Type",
          Optional = true
        }, ParamRelevance.Primary
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.BoundaryConditions()
        {
          Name = "Boundary Conditions",
          NickName = "BC",
          Description = $"Boundary Conditions list.",
          Access = GH_ParamAccess.list
        }
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.TryGetData(DA, "Model", out Types.IGH_ModelInstance model, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Type", out ARDB.Structure.BoundaryConditionsType? type)) return;

      using (var collector = new ARDB.FilteredElementCollector(model.ModelDocument.Value))
      {
        var elementsCollector = collector.WherePasses(ElementFilter);

        if (type is object)
          elementsCollector = elementsCollector.WhereParameterEqualsTo(ARDB.BuiltInParameter.BOUNDARY_CONDITIONS_TYPE, (int) type);

        DA.SetDataList
        (
          "Boundary Conditions",
          elementsCollector.
          Select(Types.BoundaryConditions.FromElement).
          AtModel(model).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
