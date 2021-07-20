using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Phase
{
  public class QueryPhases : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("256E48B1-5F4E-4EE1-BFBE-FEAB2B18833A");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "P";

    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.Phase));

    public QueryPhases() : base
    (
      name: "Query Phases",
      nickname: "Phases",
      description: "Get document construction phases list",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Name", "N", "Phase name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true, relevance: ParamRelevance.Occasional)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Phase>("Phases", "P", "Phases list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      Params.GetData(DA, "Name", out string name);

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var materialsCollector = collector.WherePasses(ElementFilter);

        if (Params.GetData(DA, "Filter", out DB.ElementFilter filter))
          materialsCollector = materialsCollector.WherePasses(filter);

        if (TryGetFilterStringParam(DB.BuiltInParameter.PHASE_NAME, ref name, out var nameFilter))
          materialsCollector = materialsCollector.WherePasses(nameFilter);

        var phases = collector.Cast<DB.Phase>();

        if (!string.IsNullOrEmpty(name))
          phases = phases.Where(x => x.Name.IsSymbolNameLike(name));

        phases = phases.OrderBy(x => x.get_Parameter(DB.BuiltInParameter.PHASE_SEQUENCE_NUMBER).AsInteger());

        DA.SetDataList("Phases", phases);
      }
    }
  }
}
