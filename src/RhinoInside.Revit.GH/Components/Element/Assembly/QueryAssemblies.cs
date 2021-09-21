using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Assemblies
{
  public class QueryAssemblies : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("fd5b45c3-7f55-4ad8-abbe-e871f95b4988");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.AssemblyInstance));

    public QueryAssemblies() : base
    (
      name: "Query Assemblies",
      nickname: "Assemblies",
      description: "Get all document assemblies",
      category: "Revit",
      subCategory: "Assembly"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Name", "N", "Assembly name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.AssemblyInstance>("Assemblies", "A", "Assembly list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      string name = null;
      DA.GetData("Name", ref name);

      DB.ElementFilter filter = null;
      DA.GetData("Filter", ref filter);

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var assemblyCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          assemblyCollector = assemblyCollector.WherePasses(filter);

        // DB.BuiltInParameter.ASSEMBLY_NAME does not return a parameter
        // using the type name instead
        if (TryGetFilterStringParam(DB.BuiltInParameter.ELEM_TYPE_PARAM, ref name, out var assemblyNameFilter))
          assemblyCollector = assemblyCollector.WherePasses(assemblyNameFilter);

        var assemblies = assemblyCollector.Cast<DB.AssemblyInstance>();

        DA.SetDataList("Assemblies", assemblies);
      }
    }
  }
}
