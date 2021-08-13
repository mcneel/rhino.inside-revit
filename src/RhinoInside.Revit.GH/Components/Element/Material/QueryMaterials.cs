using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Material
{
  public class QueryMaterials : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("94AF13C1-CE70-46B5-9103-24B46E2F7375");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "M";

    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.Material));

    public QueryMaterials() : base
    (
      name: "Query Materials",
      nickname: "Materials",
      description: "Get document materials list",
      category: "Revit",
      subCategory: "Material"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition (new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Class", "C", "Material class", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_String>("Name", "N", "Material name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Material>("Materials", "M", "Material list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      string @class = null;
      DA.GetData("Class", ref @class);

      string name = null;
      DA.GetData("Name", ref name);

      DB.ElementFilter filter = null;
      DA.GetData("Filter", ref filter);

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var materialsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          materialsCollector = materialsCollector.WherePasses(filter);

        if (TryGetFilterStringParam(DB.BuiltInParameter.MATERIAL_NAME, ref name, out var nameFilter))
          materialsCollector = materialsCollector.WherePasses(nameFilter);

        var materials = collector.Cast<DB.Material>();

        if (!string.IsNullOrEmpty(@class))
          materials = materials.Where(x => x.MaterialClass.IsSymbolNameLike(@class));

        if (!string.IsNullOrEmpty(name))
          materials = materials.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList("Materials", materials);
      }
    }
  }
}
