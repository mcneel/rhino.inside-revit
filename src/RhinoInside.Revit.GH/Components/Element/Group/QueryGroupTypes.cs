using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class QueryGroupTypes : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("97E9C6BB-8442-4F77-BCA1-6BE8AAFBDC96");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "G";

    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.GroupType));

    public QueryGroupTypes() : base
    (
      name: "Query Group Types",
      nickname: "GroupTypes",
      description: "Get document group types list",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.FromParam(DocumentComponent.CreateDocumentParam(), ParamVisibility.Voluntary),
      ParamDefinition.Create<Param_String>("Name", "N", "Group name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ElementType>("Types", "T", "Types list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      string name = null;
      DA.GetData("Name", ref name);

      DB.ElementFilter filter = null;
      DA.GetData("Filter", ref filter);

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var viewsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          viewsCollector = viewsCollector.WherePasses(filter);

        if (TryGetFilterStringParam(DB.BuiltInParameter.SYMBOL_NAME_PARAM, ref name, out var nameFilter))
          viewsCollector = viewsCollector.WherePasses(nameFilter);

        var groupTypes = collector.Cast<DB.GroupType>();

        if (!string.IsNullOrEmpty(name))
          groupTypes = groupTypes.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList("Types", groupTypes);
      }
    }
  }
}
