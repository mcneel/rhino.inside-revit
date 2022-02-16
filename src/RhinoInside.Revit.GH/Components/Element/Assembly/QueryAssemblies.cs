using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Assemblies
{
  [ComponentVersion(introduced: "1.2", updated: "1.5")]
  public class QueryAssemblies : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("fd5b45c3-7f55-4ad8-abbe-e871f95b4988");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementClassFilter(typeof(ARDB.AssemblyInstance));

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
      ParamDefinition.Create<Param_String>("Type Name", "N", "Assembly type name", GH_ParamAccess.item, optional: true),
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
      DA.GetData("Type Name", ref name);

      ARDB.ElementFilter filter = null;
      DA.GetData("Filter", ref filter);

      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var assemblyCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          assemblyCollector = assemblyCollector.WherePasses(filter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.ALL_MODEL_TYPE_NAME, ref name, out var assemblyNameFilter))
          assemblyCollector = assemblyCollector.WherePasses(assemblyNameFilter);

        var assemblies = assemblyCollector.Cast<ARDB.AssemblyInstance>();

        if (!string.IsNullOrEmpty(name))
          assemblies = assemblies.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList
        (
          "Assemblies",
          assemblies.
          Select(x => new Types.AssemblyInstance(x)).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
