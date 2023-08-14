using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  using External.DB.Extensions;
  using RhinoInside.Revit.External.DB;

  [ComponentVersion(introduced: "1.16")]
  public class QueryViewElements : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("ECC6FA17-ED92-4166-B47D-A32D50FA8475");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementIsElementTypeFilter(inverted: true);

    public QueryViewElements() : base
    (
      name: "Query View Elements",
      nickname: "V-Elements",
      description: "Get elements visible in a view",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.View>("View", "V", "View", GH_ParamAccess.item),
      ParamDefinition.Create<Parameters.Category>("Categories", "C", "Category", GH_ParamAccess.list, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Element>("Elements", "E", "Elements list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;
      if (!Params.TryGetDataList(DA, "Categories", out IList<Types.Category> categories)) return;
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter, x => x.IsValidObject)) return;

      using (var collector = new ARDB.FilteredElementCollector(view.Document, view.Id))
      {
        var elementCollector = collector.WherePasses(ElementFilter);

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
          var hiddenCategories = BuiltInCategoryExtension.GetHiddenInUIBuiltInCategories(view.Document).ToList();
          hiddenCategories.Add(ARDB.BuiltInCategory.OST_SectionBox);  // 'Section Boxes' has little sense here!?!?
          hiddenCategories.Add(ARDB.BuiltInCategory.INVALID);         // `ScheduleSheetInstance` Viewer has no Category, so we filter here

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
          Select(Types.Element.FromElement).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }

  [ComponentVersion(introduced: "1.16")]
  public class QueryViewOwnedElements : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("92B3F600-40FB-4DD3-992B-68B68D284167");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override ARDB.ElementFilter ElementFilter => CompoundElementFilter.ElementIsViewSpecificFilter();

    public QueryViewOwnedElements() : base
    (
      name: "Query View Owned Elements",
      nickname: "VO-Elements",
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
        var elementCollector = collector.OwnedByView(view.Id);

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
