using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  [ComponentVersion(introduced: "1.16")]
  public class QueryTitleBlocks : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("EC5CD3BB-B8F3-40C0-BE4C-35D8D8EE63CD");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override ARDB.ElementFilter ElementFilter => CompoundElementFilter.Intersect
    (
      CompoundElementFilter.ElementIsElementTypeFilter(inverted: true),
      new ARDB.ElementClassFilter(typeof(ARDB.FamilyInstance)),
      new ARDB.ElementCategoryFilter(ARDB.BuiltInCategory.OST_TitleBlocks)
    );

    public QueryTitleBlocks() : base
    (
      name: "Query Title Blocks",
      nickname: "TBlocks",
      description: "Get all title blocks placed in a sheet",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.ViewSheet>("Sheet", "S", "Sheet"),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", optional: true),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.FamilyInstance>("Title Blocks", "TB", "Title Blocks list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Sheet", out Types.ViewSheet sheet, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter, x => x.IsValidObject)) return;

      using (var collector = new ARDB.FilteredElementCollector(sheet.Document))
      {
        var elementCollector = collector.WherePasses(ElementFilter);

        elementCollector = elementCollector.OwnedByView(sheet.Id);

        if (filter is object)
          elementCollector = elementCollector.WherePasses(filter);

        DA.SetDataList
        (
          "Title Blocks",
          elementCollector.
          Select(Types.FamilyInstance.FromElement).
          OfType<Types.FamilyInstance>().
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
