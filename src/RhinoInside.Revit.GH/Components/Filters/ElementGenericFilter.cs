using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Filters
{
  public class ElementExcludeElementTypeFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("F69D485F-B262-4297-A496-93F5653F5D19");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "T";

    public ElementExcludeElementTypeFilter()
    : base("Exclude Types", "NoTypes", "Filter used to exclude element types", "Revit", "Filter")
    { }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      DA.SetData("Filter", new DB.ElementIsElementTypeFilter(!inverted));
    }
  }

  public class ElementClasses : Grasshopper.Special.ValueSet<GH_String>
  {
    public override Guid ComponentGuid => new Guid("F432D672-FA9D-48B1-BABB-CFF8BEF38787");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;

    protected override System.Drawing.Bitmap Icon =>
      ((System.Drawing.Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
      base.Icon;

    public ElementClasses() : base
    (
      name: "Element Classes",
      nickname: "Element Classes",
      description: "Provides a picker for Revit element classes",
      category: "Revit",
      subcategory: "Filter"
    )
    {
      IconDisplayMode = GH_IconDisplayMode.name;
    }

    static readonly HashSet<Type> NonFilterableClasses = new HashSet<Type>
    {
      typeof(DB.AreaTagType),
      typeof(DB.AnnotationSymbol),
      typeof(DB.AnnotationSymbolType),
      typeof(DB.CombinableElement),
      typeof(DB.CurveByPoints),
      typeof(DB.DetailCurve),
      typeof(DB.DetailArc),
      typeof(DB.DetailEllipse),
      typeof(DB.DetailLine),
      typeof(DB.DetailNurbSpline),
      typeof(DB.ModelCurve),
      typeof(DB.ModelArc),
      typeof(DB.ModelEllipse),
      typeof(DB.ModelLine),
      typeof(DB.ModelNurbSpline),
      typeof(DB.ModelHermiteSpline),
      typeof(DB.SymbolicCurve),
      typeof(DB.DetailCurve),
      typeof(DB.Mullion),
      typeof(DB.Panel),
      typeof(DB.SlabEdge),

      typeof(DB.Architecture.RoomTagType),
      typeof(DB.Architecture.Fascia),
      typeof(DB.Architecture.Gutter),

      typeof(DB.Mechanical.SpaceTagType),

      typeof(DB.Structure.TrussType),
      typeof(DB.Structure.AreaReinforcementCurve),
    };

    static readonly HashSet<Type> SpecialClasses = new HashSet<Type>
    {
      typeof(DB.Area),
      typeof(DB.AreaTag),

      typeof(DB.Architecture.Room),
      typeof(DB.Architecture.RoomTag),

      typeof(DB.Mechanical.Space),
      typeof(DB.Mechanical.SpaceTag),
    };

    static readonly Type[] FilterableClasses = typeof(DB.Element).
      Assembly.ExportedTypes.
      Where
      (
        x =>
        {
          if (NonFilterableClasses.Contains(x)) return false;
          if (SpecialClasses.Contains(x)) return true;
          if (x.IsSubclassOf(typeof(DB.Element)))
          {
            try { using (new DB.ElementClassFilter(x)) return true; }
            catch { }
          }
          return false;
        }
      ).
      OrderBy(x => x.FullName.Count(c => c == '.')).
      ThenBy(x => x.FullName).
      ToArray();

    protected override void LoadVolatileData()
    {
      if (SourceCount == 0)
      {
        m_data.Clear();
        m_data.AppendRange(FilterableClasses.Select(x => new GH_String(x.FullName)));
      }

      base.LoadVolatileData();
    }

    protected override void SortItems()
    {
      // FilterableClasses is already sorted alphabetically and
      // taking into account Namespace nesting level.
    }
  }

  public class ElementClassFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("6BD34014-CD73-42D8-94DB-658BE8F42254");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    protected override string IconTag => "C";

    public ElementClassFilter()
    : base("Class Filter", "ClassFltr", "Filter used to match elements by their API class", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddTextParameter("Classes", "C", "Classes to match", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var classNames = new List<string>();
      if (!DA.GetDataList("Classes", classNames))
        return;

      var filters = new List<DB.ElementFilter>();
      var classes = new HashSet<string>(classNames);
      if (classes.Remove("Autodesk.Revit.DB.Area"))
        filters.Add(new DB.AreaFilter());
      if (classes.Remove("Autodesk.Revit.DB.AreaTag"))
        filters.Add(new DB.AreaTagFilter());
      if (classes.Remove("Autodesk.Revit.DB.Architecture.Room"))
        filters.Add(new DB.Architecture.RoomFilter());
      if (classes.Remove("Autodesk.Revit.DB.Architecture.RoomTag"))
        filters.Add(new DB.Architecture.RoomTagFilter());
      if (classes.Remove("Autodesk.Revit.DB.Mechanical.Space"))
        filters.Add(new DB.Mechanical.SpaceFilter());
      if (classes.Remove("Autodesk.Revit.DB.Mechanical.SpaceTag"))
        filters.Add(new DB.Mechanical.SpaceTagFilter());

      try
      {
        var types = classes.Select(x => Type.GetType($"{x},RevitAPI", true)).ToList();

        if (types.Count > 0)
        {
          if (types.Count == 1)
            filters.Add(new DB.ElementClassFilter(types[0]));
          else
            filters.Add(new DB.ElementMulticlassFilter(types));
        }

        if (filters.Count == 0)
        {
          var nothing = new DB.ElementFilter[] { new DB.ElementIsElementTypeFilter(true), new DB.ElementIsElementTypeFilter(false) };
          DA.SetData("Filter", new DB.LogicalAndFilter(nothing));
        }
        else if (filters.Count == 1)
          DA.SetData("Filter", filters[0]);
        else
          DA.SetData("Filter", new DB.LogicalOrFilter(filters));
      }
      catch (System.TypeLoadException e)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
      }
      catch (Autodesk.Revit.Exceptions.ArgumentException e)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message.Replace(". ", $".{Environment.NewLine}"));
      }
    }
  }

  public class ElementCategoryFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("D08F7AB1-BE36-45FA-B006-0078022DB140");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "C";

    public ElementCategoryFilter()
    : base("Category Filter", "CatFltr", "Filter used to match elements by their category", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "Categories", "C", "Categories to match", GH_ParamAccess.list);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var categoryIds = new List<DB.ElementId>();
      if (!DA.GetDataList("Categories", categoryIds))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      var ids = categoryIds.Select(x => x is null ? DB.ElementId.InvalidElementId : x).ToArray();
      if (ids.Length == 0)
      {
        var nothing = new DB.ElementFilter[] { new DB.ElementIsElementTypeFilter(true), new DB.ElementIsElementTypeFilter(false) };
        DA.SetData("Filter", new DB.LogicalAndFilter(nothing));
      }
      else
      {
        if (ids.Length == 1)
          DA.SetData("Filter", new DB.ElementCategoryFilter(ids[0], inverted));
        else
          DA.SetData("Filter", new DB.ElementMulticategoryFilter(ids, inverted));
      }
    }
  }

  public class ElementTypeFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("4434C470-4CAF-4178-929D-284C3B5A24B5");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "T";

    public ElementTypeFilter()
    : base("Type Filter", "TypeFltr", "Filter used to match elements by their type", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "Types", "T", "Types to match", GH_ParamAccess.list);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var types = new List<DB.ElementType>();
      if (!DA.GetDataList("Types", types))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      if (types.Any())
      {
        var provider = new DB.ParameterValueProvider(new DB.ElementId(DB.BuiltInParameter.ELEM_TYPE_PARAM));

        var typeIds = types.Select(x => x?.Id ?? DB.ElementId.InvalidElementId).ToArray();
        if (typeIds.Length == 1)
        {
          var rule = new DB.FilterElementIdRule(provider, new DB.FilterNumericEquals(), typeIds[0]);
          var filter = new DB.ElementParameterFilter(rule, inverted) as DB.ElementFilter;

          DA.SetData("Filter", filter);
        }
        else
        {
          if (inverted)
          {
            var rules = typeIds.Select(x => new DB.FilterInverseRule(new DB.FilterElementIdRule(provider, new DB.FilterNumericEquals(), x))).ToArray();
            DA.SetData("Filter", new DB.ElementParameterFilter(rules));
          }
          else
          {
            var filters = typeIds.Select(x => new DB.FilterElementIdRule(provider, new DB.FilterNumericEquals(), x)).Select(x => new DB.ElementParameterFilter(x)).ToArray();
            DA.SetData("Filter", new DB.LogicalOrFilter(filters));
          }
        }
      }
    }
  }

  public class ElementParameterFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("E6A1F501-BDA4-4B78-8828-084B5EDA926F");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "#";

    public ElementParameterFilter()
    : base("Parameter Filter", "ParaFltr", "Filter used to match elements by the value of a parameter", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.FilterRule(), "Rules", "R", "Rules to check", GH_ParamAccess.list);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var rules = new List<DB.FilterRule>();
      if (!DA.GetDataList("Rules", rules))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      rules = rules.OfType<DB.FilterRule>().ToList();
      if (rules.Count > 0)
        DA.SetData("Filter", new DB.ElementParameterFilter(rules, inverted));
    }
  }
}
