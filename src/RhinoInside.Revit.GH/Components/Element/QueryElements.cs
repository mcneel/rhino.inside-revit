using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class QueryElement : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("BBCF5D5C-9ABC-4E28-861A-584644EDFC3D");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;

    public QueryElement() : base
    (
      name: "Query Element",
      nickname: "Element",
      description: "Get element by ID",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_GenericObject>("Id", "ID", "Element Id or UniqueId to look for", defaultValue: -1),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Element>("Element", "E", string.Empty),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc)) return;
      if (!Params.GetData(DA, "Id", out Grasshopper.Kernel.Types.IGH_Goo id)) return;

      switch (id)
      {
        case Grasshopper.Kernel.Types.GH_Number n:
          if (!double.IsNaN(n.Value))
          {
            try { DA.SetData("Element", Types.Element.FromElementId(doc, new DB.ElementId(System.Convert.ToInt32(n.Value)))); }
            catch { }
          }
          break;

        case Grasshopper.Kernel.Types.GH_Integer i:
          DA.SetData("Element", Types.Element.FromElementId(doc, new DB.ElementId(i.Value)));
          break;

        case Grasshopper.Kernel.Types.GH_String s:

          if (FullUniqueId.TryParse(s.Value, out var documentId, out var uniqueId))
          {
            if (doc.GetFingerprintGUID() == documentId && doc.TryGetElementId(uniqueId, out var elementId))
              DA.SetData("Element", Types.Element.FromElementId(doc, elementId));

            return;
          }

          if (UniqueId.TryParse(s.Value, out var _, out var _))
          {
            if (doc.TryGetElementId(s.Value, out var elementId))
              DA.SetData("Element", Types.Element.FromElementId(doc, elementId));

            return;
          }

          if (int.TryParse(s.Value, out var index))
          {
            if(doc.GetElement(new DB.ElementId(index)) is DB.Element element)
              DA.SetData("Element", Types.Element.FromElement(element));

            return;
          }

          break;

        case Types.CategoryId c:
          DA.SetData("Element", Types.Category.FromElementId(doc, new DB.ElementId(c.Value)));
          break;

        case Types.ParameterId p:
          DA.SetData("Element", Types.ParameterKey.FromElementId(doc, new DB.ElementId(p.Value)));
          break;
      }
    }
  }

  public class QueryElements : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("0F7DA57E-6C05-4DD0-AABF-69E42DF38859");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override DB.ElementFilter ElementFilter => new DB.ElementIsElementTypeFilter(true);

    static readonly string[] keywords = new string[] { "Count" };
    public override IEnumerable<string> Keywords => Enumerable.Concat(base.Keywords, keywords);

    public QueryElements() : base
    (
      name: "Query Elements",
      nickname: "Elements",
      description: "Get document model elements list",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item),
      ParamDefinition.Create<Param_Integer>("Limit", "L", $"Max number of Elements to query for.{Environment.NewLine}For an unlimited query remove this parameter.", defaultValue: 100, GH_ParamAccess.item, relevance: ParamRelevance.Primary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Element>("Elements", "E", "Elements list", GH_ParamAccess.list, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Integer>("Count", "C", $"Elements count.{Environment.NewLine}For a more performant way of knowing how many elements this query returns remove the Elements output.", GH_ParamAccess.item, relevance: ParamRelevance.Primary),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc)) return;
      if (!Params.GetData(DA, "Filter", out Types.ElementFilter filter, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Limit", out int? limit, x => x >= 0)) return;

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var elementCollector = collector.WherePasses(ElementFilter).WherePasses(filter.Value);
        var elementCount = elementCollector.GetElementCount();

        Params.TrySetDataList(DA, "Elements", () =>
        {
          IEnumerable<DB.Element> elements = elementCollector;
          if (limit.HasValue)
          {
            if (elementCount > limit)
            {
              var _Elements_ = Params.IndexOfOutputParam("Elements");
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"'{Params.Output[_Elements_].NickName}' contains only first {limit} of {elementCount} total elements.");
            }

            elements = elements.Take(limit.Value);
          }

          return elements.Convert(Types.Element.FromElement);
        });

        Params.TrySetData(DA, "Count", () => elementCount);
      }
    }
  }

  public class QueryViewElements : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("79DAEA3A-13A3-49BF-8BEB-AA28E3BE4515");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override DB.ElementFilter ElementFilter => new DB.ElementIsElementTypeFilter(true);

    public QueryViewElements() : base
    (
      name: "Query View Elements",
      nickname: "ViewEles",
      description: "Get elements visible in a view",
      category: "Revit",
      subCategory: "Element"
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
      ParamDefinition.Create<Parameters.GraphicalElement>("Elements", "E", "Elements list", GH_ParamAccess.list)
    };

    static DB.ElementFilter ElementCategoriesFilter(DB.Document doc, DB.ElementId[] ids)
    {
      var list = new List<DB.ElementFilter>();

      if (ids.Length == 1)    list.Add(new DB.ElementCategoryFilter(ids[0]));
      else if(ids.Length > 1) list.Add(new DB.ElementMulticategoryFilter(ids));

      if (doc.IsFamilyDocument)
      {
        foreach (var id in ids)
        {
          using (var provider = new DB.ParameterValueProvider(new DB.ElementId(DB.BuiltInParameter.FAMILY_ELEM_SUBCATEGORY)))
          using (var evaluator = new DB.FilterNumericEquals())
          using (var rule = new DB.FilterElementIdRule(provider, evaluator, id))
            list.Add(new DB.ElementParameterFilter(rule));
        }
      }

      if (list.Count == 0)
      {
        var nothing = new DB.ElementFilter[] { new DB.ElementIsElementTypeFilter(true), new DB.ElementIsElementTypeFilter(false) };
        return new DB.LogicalAndFilter(nothing);
      }
      else if (list.Count == 1)
      {
        return list[0];
      }
      else
      {
        return new DB.LogicalOrFilter(list);
      }
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;
      if (!Params.TryGetDataList(DA, "Categories", out IList<Types.Category> categories)) return;
      if (!Params.TryGetData(DA, "Filter", out DB.ElementFilter filter, x => x.IsValidObject)) return;

      using (var collector = new DB.FilteredElementCollector(view.Document, view.Id))
      {
        var elementCollector = collector.WherePasses(ElementFilter);

        if (categories is object)
        {
          var ids = categories.
            Where(x => x.IsValid && x.Document.Equals(view.Document)).
            Select(x => x.Id).ToArray();

          elementCollector = elementCollector.WherePasses(ElementCategoriesFilter(view.Document, ids));
        }

        if (filter is object)
          elementCollector = elementCollector.WherePasses(filter);

        DA.SetDataList
        (
          "Elements",
          elementCollector.
          Where(x => Types.GraphicalElement.IsValidElement(x)).
          Convert(Types.Element.FromElement)
        );
      }
    }
  }
}
