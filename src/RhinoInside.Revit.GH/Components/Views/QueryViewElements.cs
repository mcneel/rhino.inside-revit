using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.16")]
  public class QueryViewElements : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("92B3F600-40FB-4DD3-992B-68B68D284167");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementIsElementTypeFilter(inverted: true);

    public QueryViewElements() : base
    (
      name: "Query View Owned Elements",
      nickname: "ViewEles",
      description: "Get elements owned by a view",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.View>("View", "V", "View"),
      ParamDefinition.Create<Parameters.Category>("Categories", "C", "Category", GH_ParamAccess.list, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", optional: true),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.GraphicalElement>("Elements", "E", "Elements list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;
      if (!Params.TryGetDataList(DA, "Categories", out IList<Types.Category> categories)) return;
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter, x => x.IsValidObject)) return;

      using (var collector = new ARDB.FilteredElementCollector(view.Document))
      {
        var elementCollector = collector.WherePasses(ElementFilter);

        elementCollector = elementCollector.OwnedByView(view.Id);

        if (categories is object)
        {
          var ids = categories.
            Where(x => x is object && (x.IsEmpty || x.IsValid) && (x.Document is null || x.Document.Equals(view.Document))).
            Select(x => x.Id).
            ToList();

          elementCollector = elementCollector.WherePasses
          (
            ERDB.CompoundElementFilter.ElementCategoryFilter(ids, inverted: false, view.Document.IsFamilyDocument)
          );
        }
        else
        {
          // Default category filtering
          var hiddenCategories = BuiltInCategoryExtension.GetHiddenInUIBuiltInCategories(view.Document).
            Append(ARDB.BuiltInCategory.INVALID). // `ScheduleSheetInstance` Viewer has no Category, so we filter here
            ToList();

          elementCollector = elementCollector.WherePasses
          (
            new ARDB.ElementMulticategoryFilter(hiddenCategories, inverted: true)
          );
        }

        if (filter is object)
          elementCollector = elementCollector.WherePasses(filter);

        DA.SetDataList
        (
          "Elements",
          elementCollector.
          Select(Types.GraphicalElement.FromElement).
          OfType<Types.GraphicalElement>().
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
