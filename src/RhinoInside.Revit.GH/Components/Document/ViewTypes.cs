using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentViewTypes : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("51E306BD-4736-4B7D-B2FF-B23E0717EEBB");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "V";

    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.ViewFamilyType));

    public DocumentViewTypes() : base
    (
      name: "View Types",
      nickname: "ViewTypes",
      description: "Get document view types list",
      category: "Revit",
      subCategory: "Query"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.FromParam(DocumentComponent.CreateDocumentParam(), ParamVisibility.Voluntary),
      ParamDefinition.Create<Parameters.Param_Enum<Types.ViewFamily>>("Family", "F", string.Empty, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_String>("Name", "N", "View Type name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ElementType>("Types", "T", "Views Types list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      var viewFamily = DB.ViewFamily.Invalid;
      DA.GetData("Family", ref viewFamily);

      string name = null;
      DA.GetData("Name", ref name);

      var filter = default(DB.ElementFilter);
      DA.GetData("Filter", ref filter);

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var elementCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          elementCollector = elementCollector.WherePasses(filter);

        if (TryGetFilterStringParam(DB.BuiltInParameter.SYMBOL_NAME_PARAM, ref name, out var nameFilter))
          elementCollector = elementCollector.WherePasses(nameFilter);

        var elements = collector.Cast<DB.ViewFamilyType>();

        if (viewFamily != DB.ViewFamily.Invalid)
          elements = elements.Where(x => x.ViewFamily == viewFamily);

        if (!string.IsNullOrEmpty(name))
          elements = elements.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList("Types", elements);
      }
    }
  }
}
