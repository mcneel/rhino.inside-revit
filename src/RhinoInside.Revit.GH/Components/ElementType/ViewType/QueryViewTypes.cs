using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  public class QueryViewTypes : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("51E306BD-4736-4B7D-B2FF-B23E0717EEBB");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "V";

    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementClassFilter(typeof(ARDB.ViewFamilyType));

    public QueryViewTypes() : base
    (
      name: "Query View Types",
      nickname: "ViewTypes",
      description: "Get document view types list",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Parameters.Param_Enum<Types.ViewFamily>>("Family", "F", string.Empty, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_String>("Name", "N", "View Type name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ElementType>("Types", "T", "Views Types list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      var viewFamily = ARDB.ViewFamily.Invalid;
      DA.GetData("Family", ref viewFamily);

      string name = null;
      DA.GetData("Name", ref name);

      var filter = default(ARDB.ElementFilter);
      DA.GetData("Filter", ref filter);

      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var elementCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          elementCollector = elementCollector.WherePasses(filter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.ALL_MODEL_TYPE_NAME, ref name, out var nameFilter))
          elementCollector = elementCollector.WherePasses(nameFilter);

        var types = collector.Cast<ARDB.ViewFamilyType>();

        if (viewFamily != ARDB.ViewFamily.Invalid)
          types = types.Where(x => x.ViewFamily == viewFamily);

        if (!string.IsNullOrEmpty(name))
          types = types.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList
        (
          "Types",
          types.
          Select(Types.ElementType.FromElement).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
