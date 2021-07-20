using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;
using EDBS = RhinoInside.Revit.External.DB.Schemas;

namespace RhinoInside.Revit.GH.Components
{
  public class FilterElement : Component
  {
    public override Guid ComponentGuid => new Guid("36180A9E-04CA-4B38-82FE-C6707B32C680");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "Y";

    public FilterElement() : base
    (
      name: "Filter Element",
      nickname: "FiltElem",
      description: "Evaluate if an Element pass a Filter",
      category: "Revit",
      subCategory: "Filter"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Elements", "E", "Elements to filter", GH_ParamAccess.list);
      manager.AddParameter(new Parameters.ElementFilter(), "Filter", "F", "Filter to apply", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddBooleanParameter("Pass", "P", "True if the input Element is accepted by the Filter. False if the element is rejected.", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var elements = new List<Types.IGH_Element>();
      if (!DA.GetDataList("Elements", elements))
        return;

      var filter = default(Types.ElementFilter);
      if (!DA.GetData("Filter", ref filter))
        return;

      DA.SetDataList
      (
        "Pass",
        elements.Select(x => x.IsValid ? new GH_Boolean(filter.Value.PassesFilter(x.Document, x.Id)) : default)
      );
    }
  }

  public abstract class ElementFilterComponent : Component
  {
    protected ElementFilterComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddBooleanParameter("Inverted", "I", "True if the results of the filter should be inverted", GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementFilter(), "Filter", "F", string.Empty, GH_ParamAccess.item);
    }
  }

  #region Primary
  public class ElementExclusionFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("396F7E91-7F08-4A3D-9B9B-B6AA91AC0A2B");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "⊄";

    public ElementExclusionFilter()
    : base("Exclusion Filter", "Exclude", "Filter used to exclude a set of elements", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Elements", "E", "Elements to exclude", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var elementIds = new List<DB.ElementId>();
      if (!DA.GetDataList("Elements", elementIds))
        return;

      var ids = elementIds.Where(x => x is object).ToArray();
      if (ids.Length > 0)
        DA.SetData("Filter", new DB.ExclusionFilter(ids));
    }
  }

  public abstract class ElementLogicalFilter : Component, IGH_VariableParameterComponent
  {
    protected ElementLogicalFilter(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory)
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementFilter(), "Filter A", "A", string.Empty, GH_ParamAccess.item);
      manager.AddParameter(new Parameters.ElementFilter(), "Filter B", "B", string.Empty, GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementFilter(), "Filter", "F", string.Empty, GH_ParamAccess.item);
    }

    static int ToIndex(char value) => value - 'A';
    static char ToChar(int value) => (char) ('A' + value);

    public bool CanInsertParameter(GH_ParameterSide side, int index)
    {
      return side == GH_ParameterSide.Input && index <= ToIndex('Z') && index == Params.Input.Count;
    }

    public bool CanRemoveParameter(GH_ParameterSide side, int index)
    {
      return side == GH_ParameterSide.Input && index > ToIndex('B') && index == Params.Input.Count - 1;
    }

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      if (side == GH_ParameterSide.Output) return default;

      var name = $"Filter {ToChar(index)}";
      var nickName = ToChar(index).ToString();
      return new Parameters.ElementFilter()
      {
        Name = name,
        NickName = Grasshopper.CentralSettings.CanvasFullNames ? name : nickName
      };
    }

    public bool DestroyParameter(GH_ParameterSide side, int index) => CanRemoveParameter(side, index);
    public void VariableParameterMaintenance() { }

    public override void AddedToDocument(GH_Document document)
    {
      Grasshopper.CentralSettings.CanvasFullNamesChanged += CentralSettings_CanvasFullNamesChanged;
      base.AddedToDocument(document);
    }

    public override void RemovedFromDocument(GH_Document document)
    {
      Grasshopper.CentralSettings.CanvasFullNamesChanged -= CentralSettings_CanvasFullNamesChanged;
      base.RemovedFromDocument(document);
    }

    private void CentralSettings_CanvasFullNamesChanged()
    {
      for (int i = 0; i < Params.Input.Count; ++i)
      {
        var param = Params.Input[i];
        var name = $"Filter {ToChar(i)}";
        var nickName = ToChar(i).ToString();

        if (Grasshopper.CentralSettings.CanvasFullNames)
        {
          if (param.NickName == nickName)
            param.NickName = name;
        }
        else
        {
          if (param.NickName == name)
            param.NickName = nickName;
        }
      }
    }
  }

  public class ElementLogicalAndFilter : ElementLogicalFilter
  {
    public override Guid ComponentGuid => new Guid("0E534AFB-7264-4AFF-99F3-7F7EA7DB9F3D");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "∧";

    public ElementLogicalAndFilter()
    : base("Logical And Filter", "AndFltr", "Filter used to combine a set of filters that pass when any pass", "Revit", "Filter")
    { }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var filters = new List<DB.ElementFilter>();
      for (int i = 0; i < Params.Input.Count; ++i)
      {
        DB.ElementFilter filter = default;
        if (DA.GetData(i, ref filter) && filter is object)
          filters.Add(filter);
      }

      if (filters.Count > 0)
        DA.SetData("Filter", new DB.LogicalAndFilter(filters));
    }
  }

  public class ElementLogicalOrFilter : ElementLogicalFilter
  {
    public override Guid ComponentGuid => new Guid("3804757F-3F4C-469D-8788-FCA26F477A9C");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "∨";

    public ElementLogicalOrFilter()
    : base("Logical Or Filter", "OrFltr", "Filter used to combine a set of filters that pass when any pass", "Revit", "Filter")
    { }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var filters = new List<DB.ElementFilter>();
      for (int i = 0; i < Params.Input.Count; ++i)
      {
        DB.ElementFilter filter = default;
        if (DA.GetData(i, ref filter) && filter is object)
          filters.Add(filter);
      }

      if (filters.Count > 0)
        DA.SetData("Filter", new DB.LogicalOrFilter(filters));
    }
  }
  #endregion

  #region Secondary
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
          if(x.IsSubclassOf(typeof(DB.Element)))
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
      if(rules.Count > 0)
        DA.SetData("Filter", new DB.ElementParameterFilter(rules, inverted));
    }
  }
  #endregion

  #region Tertiary
  public class ElementBoundingBoxFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("3B8BE676-390B-4BE1-B6DA-C02FFA3234B6");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "B";

    public ElementBoundingBoxFilter()
    : base("Bounding Box Filter", "BBoxFltr", "Filter used to match elements by their BoundingBox", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddGeometryParameter("Bounding Box", "B", "World aligned bounding box to query", GH_ParamAccess.list);
      manager.AddBooleanParameter("Union", "U", "Target union of bounding boxes.", GH_ParamAccess.item, true);
      manager.AddBooleanParameter("Strict", "S", "True means element should be strictly contained", GH_ParamAccess.item, false);
      manager.AddNumberParameter("Tolerance", "T", "Tolerance used to query", GH_ParamAccess.item, 0.0);
      base.RegisterInputParams(manager);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      base.RegisterOutputParams(manager);
      manager.AddBoxParameter("Target", "T", string.Empty, GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var geometries = new List<IGH_GeometricGoo>();
      if (!DA.GetDataList("Bounding Box", geometries))
        return;

      var union = true;
      if (!DA.GetData("Union", ref union))
        return;

      var strict = true;
      if (!DA.GetData("Strict", ref strict))
        return;

      var tolerance = 0.0;
      if (!DA.GetData("Tolerance", ref tolerance))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      var targets = new List<Rhino.Geometry.Box>();
      DB.ElementFilter filter = null;

      var boundingBoxes = geometries.Select(x => x?.Boundingbox ?? Rhino.Geometry.BoundingBox.Empty).Where(x => x.IsDegenerate(0.0) < 4);
      if (boundingBoxes.Any())
      {
        if (union)
        {
          var bbox = Rhino.Geometry.BoundingBox.Empty;
          foreach (var boundingBox in boundingBoxes)
            bbox.Union(boundingBox);

          {
            var target = new Rhino.Geometry.Box(bbox);
            target.Inflate(tolerance);
            targets.Add(target);
          }

          if (bbox.IsDegenerate(0.0) == 3)
            filter = new DB.BoundingBoxContainsPointFilter(bbox.Center.ToXYZ(), Math.Abs(tolerance) / Revit.ModelUnits, inverted);
          else if (strict)
            filter = new DB.BoundingBoxIsInsideFilter(bbox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
          else
            filter = new DB.BoundingBoxIntersectsFilter(bbox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
        }
        else
        {
          var filters = boundingBoxes.Select<Rhino.Geometry.BoundingBox, DB.ElementFilter>
          (
            x =>
            {
              {
                var target = new Rhino.Geometry.Box(x);
                target.Inflate(tolerance);
                targets.Add(target);
              }

              var bbox = x;
              var degenerate = bbox.IsDegenerate(0.0);
              if (degenerate == 3)
                return new DB.BoundingBoxContainsPointFilter(bbox.Center.ToXYZ(), Math.Abs(tolerance) / Revit.ModelUnits, inverted);
              else if (strict)
                return new DB.BoundingBoxIsInsideFilter(bbox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
              else
                return new DB.BoundingBoxIntersectsFilter(bbox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
            }
          );

          var filterList = filters.ToArray();
          filter = filterList.Length == 1 ?
                   filterList[0] :
                   new DB.LogicalOrFilter(filterList);
        }
      }

      DA.SetData("Filter", filter);
      DA.SetDataList("Target", targets);
    }
  }

  public class ElementIntersectsElementFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("D1E4C98D-E550-4F25-991A-5061EF845C37");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "I";

    public ElementIntersectsElementFilter()
    : base("Intersects Element Filter", "ElemFltr", "Filter used to match elements that intersect to the given element", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Element", "E", "Element to match", GH_ParamAccess.item);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Element element = null;
      if (!DA.GetData("Element", ref element))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      DA.SetData("Filter", new DB.ElementIntersectsElementFilter(element, inverted));
    }
  }

  public class ElementIntersectsBrepFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("A8889824-F607-4465-B84F-16DF79DD08AB");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "I";

    public ElementIntersectsBrepFilter()
    : base("Intersects Brep Filter", "BrepFltr", "Filter used to match elements that intersect to the given brep", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddBrepParameter("Brep", "B", "Brep to match", GH_ParamAccess.item);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      Rhino.Geometry.Brep brep = null;
      if (!DA.GetData("Brep", ref brep))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      if (brep.ToSolid() is DB.Solid solid)
        DA.SetData("Filter", new DB.ElementIntersectsSolidFilter(solid, inverted));
      else
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to convert Brep");
    }
  }

  public class ElementIntersectsMeshFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("09F9E451-F6C9-42FB-90E3-85E9923998A2");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "I";

    public ElementIntersectsMeshFilter()
    : base("Intersects Mesh Filter", "MeshFltr", "Filter used to match elements that intersect to the given mesh", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddMeshParameter("Mesh", "B", "Mesh to match", GH_ParamAccess.item);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      Rhino.Geometry.Mesh mesh = null;
      if (!DA.GetData("Mesh", ref mesh))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      if(mesh.ToSolid() is DB.Solid solid)
        DA.SetData("Filter", new DB.ElementIntersectsSolidFilter(solid, inverted));
      else
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to convert Mesh");
    }
  }
  #endregion

  #region Quarternary
  public class ElementLevelFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("B534489B-1367-4ACA-8FD8-D4B365CEEE0D");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "L";

    public ElementLevelFilter()
    : base("Level Filter", "LevelFltr", "Filter used to match elements associated to the given level", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Level(), "Levels", "L", "Levels to match", GH_ParamAccess.list);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var levels = new List<DB.Level>();
      if (!DA.GetDataList("Levels", levels))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      if (levels.Count == 0)
      {
        var nothing = new DB.ElementFilter[] { new DB.ElementIsElementTypeFilter(true), new DB.ElementIsElementTypeFilter(false) };
        DA.SetData("Filter", new DB.LogicalAndFilter(nothing));
      }
      else if (levels.Count == 1)
      {
        DA.SetData("Filter", new DB.ElementLevelFilter(levels[0]?.Id ?? DB.ElementId.InvalidElementId, inverted));
      }
      else
      {
        var filters = levels.Select(x => new DB.ElementLevelFilter(x?.Id ?? DB.ElementId.InvalidElementId, inverted)).ToList<DB.ElementFilter>();
        DA.SetData("Filter", new DB.LogicalOrFilter(filters));
      }
    }
  }

  public class ElementDesignOptionFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("1B197E82-3A65-43D4-AE47-FD25E4E6F2E5");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "D";

    public ElementDesignOptionFilter()
    : base("Design Option Filter", "DOptFiltr", "Filter used to match elements associated to the given Design Option", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager[manager.AddParameter(new Parameters.Element(), "Design Option", "DO", "Design Option to match", GH_ParamAccess.item)].Optional = true;
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var designOption = default(DB.DesignOption);
      var _DesignOption_ = Params.IndexOfInputParam("Design Option");
      if
      (
        Params.Input[_DesignOption_].DataType != GH_ParamData.@void &&
        !DA.GetData(_DesignOption_, ref designOption)
      )
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      DA.SetData("Filter", new DB.ElementDesignOptionFilter(designOption?.Id ?? DB.ElementId.InvalidElementId, inverted));
    }
  }

  public class ElementPhaseStatusFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("805C21EE-5481-4412-A06C-7965761737E8");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "P";

    public ElementPhaseStatusFilter()
    : base("Phase Status Filter", "PhStFiltr", "Filter used to match elements associated to the given Phase status", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Phase(), "Phase", "P", "Phase to match", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Param_Enum<Types.ElementOnPhaseStatus>(), "Status", "S", "Phase status to match", GH_ParamAccess.list);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var phase = default(DB.Phase);
      if (!DA.GetData("Phase", ref phase))
        return;

      var status = new List<DB.ElementOnPhaseStatus>();
      if (!DA.GetDataList("Status", status))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      DA.SetData("Filter", new DB.ElementPhaseStatusFilter(phase.Id, status, inverted));
    }
  }

  public class ElementOwnerViewFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("CFB42D90-F9D4-4601-9EEF-C624E92A424D");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "V";

    public ElementOwnerViewFilter()
    : base("Owner View Filter", "OViewFltr", "Filter used to match elements associated to the given View", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager[manager.AddParameter(new Parameters.View(), "View", "V", "View to match", GH_ParamAccess.item)].Optional = true;
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var view = default(DB.View);
      var _View_ = Params.IndexOfInputParam("View");
      if
      (
        Params.Input[_View_].DataType != GH_ParamData.@void &&
        !DA.GetData(_View_, ref view)
      )
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      DA.SetData("Filter", new DB.ElementOwnerViewFilter(view?.Id ?? DB.ElementId.InvalidElementId, inverted));
    }
  }

  public class ElementSelectableInViewFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("AC546F16-C917-4CD1-9F8A-FBDD6330EB80");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "S";

    public ElementSelectableInViewFilter()
    : base("Selectable In View Filter", "SelFltr", "Filter used to match seletable elements into the given View", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.View(), "View", "V", "View to match", GH_ParamAccess.item);
      base.RegisterInputParams(manager);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var view = default(DB.View);
      if(!DA.GetData("View", ref view))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      DA.SetData("Filter", new Autodesk.Revit.UI.Selection.SelectableInViewFilter(view.Document, view.Id, inverted));
    }
  }
  #endregion

  #region Quinary
  public abstract class ElementFilterRule : Component
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary;
    public override bool IsPreviewCapable => false;

    protected ElementFilterRule(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ParameterKey(), "ParameterKey", "K", "Parameter to check", GH_ParamAccess.item);
      manager.AddGenericParameter("Value", "V", "Value to check with", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.FilterRule(), "Rule", "R", string.Empty, GH_ParamAccess.item);
    }

    static readonly Dictionary<DB.BuiltInParameter, EDBS.DataType> BuiltInParametersTypes = new Dictionary<DB.BuiltInParameter, EDBS.DataType>();

    internal static bool TryGetParameterDefinition(DB.Document doc, DB.ElementId id, out DB.StorageType storageType, out EDBS.DataType dataType)
    {
      if (id.TryGetBuiltInParameter(out var builtInParameter))
      {
        storageType = doc.get_TypeOfStorage(builtInParameter);

        if (storageType == DB.StorageType.ElementId)
        {
          dataType = EDBS.SpecType.Int.Integer;
          return true;
        }

        if (storageType == DB.StorageType.Double)
        {
          if (BuiltInParametersTypes.TryGetValue(builtInParameter, out dataType))
            return true;

          var categoriesWhereDefined = doc.GetBuiltInCategoriesWithParameters().
            Select(bic => new DB.ElementId(bic)).
            Where(cid => DB.TableView.GetAvailableParameters(doc, cid).Contains(id)).
            ToArray();

          using (var collector = new DB.FilteredElementCollector(doc))
          {
            using
            (
              var filteredCollector = categoriesWhereDefined.Length == 0 ?
              collector.WherePasses(new DB.ElementClassFilter(typeof(DB.ParameterElement), false)) :
              categoriesWhereDefined.Length > 1 ?
                collector.WherePasses(new DB.ElementMulticategoryFilter(categoriesWhereDefined)) :
                collector.WherePasses(new DB.ElementCategoryFilter(categoriesWhereDefined[0]))
            )
            {
              foreach (var element in filteredCollector)
              {
                var parameter = element.get_Parameter(builtInParameter);
                if (parameter is null)
                  continue;

                dataType = parameter.Definition.GetDataType();
                BuiltInParametersTypes.Add(builtInParameter, dataType);
                return true;
              }
            }
          }

          dataType = EDBS.DataType.Empty;
          return false;
        }

        dataType = EDBS.DataType.Empty;
        return true;
      }
      else
      {
        try
        {
          if (doc.GetElement(id) is DB.ParameterElement parameter)
          {
            dataType = parameter.GetDefinition().GetDataType();
            storageType = dataType.ToStorageType();
            return true;
          }
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }
      }

      storageType = DB.StorageType.None;
      dataType = EDBS.SpecType.Empty;
      return false;
    }

    protected enum ConditionType
    {
      NotEquals,
      Equals,
      Greater,
      GreaterOrEqual,
      Less,
      LessOrEqual
    }

    protected abstract ConditionType Condition { get; }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var parameterKey = default(Types.ParameterKey);
      if (!DA.GetData("ParameterKey", ref parameterKey))
        return;

      if (!TryGetParameterDefinition(parameterKey.Document, parameterKey.Id, out var storageType, out var dataType))
      {
        if (parameterKey.Id.TryGetBuiltInParameter(out var builtInParameter))
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to found parameter '{DB.LabelUtils.GetLabelFor(builtInParameter)}' in Revit document.");
        else
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to found parameter '{parameterKey.Id.IntegerValue}' in Revit document.");

        return;
      }

      var provider = new DB.ParameterValueProvider(parameterKey.Id);

      DB.FilterRule rule = null;
      if (storageType == DB.StorageType.String)
      {
        DB.FilterStringRuleEvaluator ruleEvaluator = null;
        switch (Condition)
        {
          case ConditionType.NotEquals:
          case ConditionType.Equals:          ruleEvaluator = new DB.FilterStringEquals(); break;
          case ConditionType.Greater:         ruleEvaluator = new DB.FilterStringGreater(); break;
          case ConditionType.GreaterOrEqual:  ruleEvaluator = new DB.FilterStringGreaterOrEqual(); break;
          case ConditionType.Less:            ruleEvaluator = new DB.FilterStringLess(); break;
          case ConditionType.LessOrEqual:     ruleEvaluator = new DB.FilterStringLessOrEqual(); break;
        }

        var goo = default(GH_String);
        if (DA.GetData("Value", ref goo))
          rule = new DB.FilterStringRule(provider, ruleEvaluator, goo.Value, true);
      }
      else
      {
        DB.FilterNumericRuleEvaluator ruleEvaluator = null;
        switch (Condition)
        {
          case ConditionType.NotEquals:
          case ConditionType.Equals:          ruleEvaluator = new DB.FilterNumericEquals(); break;
          case ConditionType.Greater:         ruleEvaluator = new DB.FilterNumericGreater(); break;
          case ConditionType.GreaterOrEqual:  ruleEvaluator = new DB.FilterNumericGreaterOrEqual(); break;
          case ConditionType.Less:            ruleEvaluator = new DB.FilterNumericLess(); break;
          case ConditionType.LessOrEqual:     ruleEvaluator = new DB.FilterNumericLessOrEqual(); break;
        }

        switch (storageType)
        {
          case DB.StorageType.Integer:
          {
            var goo = default(GH_Integer);
            if (DA.GetData("Value", ref goo))
              rule = new DB.FilterIntegerRule(provider, ruleEvaluator, goo.Value);
          }
          break;

          case DB.StorageType.Double:
          {
            var goo = default(GH_Number);
            if (DA.GetData("Value", ref goo))
            {
              var value = goo.Value;
              var tol = 0.0;

              // If is a Measurable it may need to be scaled.
              if (EDBS.SpecType.IsMeasurableSpec(dataType, out var spec))
              {
                // Adjust value acording to data-type dimensionality
                if (spec.TryGetLengthDimensionality(out var dimensionality))
                  value = UnitConverter.Convert
                  (
                    value,
                    UnitConverter.ExternalUnitSystem,
                    UnitConverter.InternalUnitSystem,
                    dimensionality
                  );
                else
                  dimensionality = 0;

                // Adjust tolerance acording to data-type dimensionality
                if (Condition == ConditionType.Equals || Condition == ConditionType.NotEquals)
                {
                  tol = dimensionality == 0 ?
                    1e-6 :
                    UnitConverter.Convert
                    (
                      Revit.VertexTolerance,
                      UnitConverter.ExternalUnitSystem,
                      UnitConverter.InternalUnitSystem,
                      Math.Abs(dimensionality)
                    );
                }
              }

              rule = new DB.FilterDoubleRule(provider, ruleEvaluator, value, tol);
            }
          }
          break;

          case DB.StorageType.ElementId:
          {
            var value = default(DB.ElementId);
            if (DA.GetData("Value", ref value))
              rule = new DB.FilterElementIdRule(provider, ruleEvaluator, value);
          }
          break;
        }
      }

      if (rule is object)
      {
        if(Condition == ConditionType.NotEquals)
          DA.SetData("Rule", new DB.FilterInverseRule(rule));
        else
          DA.SetData("Rule", rule);
      }
    }
  }

  public class ElementFilterRuleNotEquals : ElementFilterRule
  {
    public override Guid ComponentGuid => new Guid("6BBE9731-EF71-42E8-A880-1D2ADFEB9F79");
    protected override string IconTag => "≠";
    protected override ConditionType Condition => ConditionType.NotEquals;

    public ElementFilterRuleNotEquals()
    : base("Not Equals Rule", "NotEquals", "Filter used to match elements if value of a parameter are not equals to Value", "Revit", "Filter")
    { }
  }

  public class ElementFilterRuleEquals : ElementFilterRule
  {
    public override Guid ComponentGuid => new Guid("0F9139AC-2A21-474C-9C5B-6864B2F2313C");
    protected override string IconTag => "=";
    protected override ConditionType Condition => ConditionType.Equals;

    public ElementFilterRuleEquals()
    : base("Equals Rule", "Equals", "Filter used to match elements if value of a parameter equals to Value", "Revit", "Filter")
    { }
  }

  public class ElementFilterRuleGreater : ElementFilterRule
  {
    public override Guid ComponentGuid => new Guid("BB7D39DA-97AD-4277-82C7-010AF857FF03");
    protected override string IconTag => ">";
    protected override ConditionType Condition => ConditionType.Greater;

    public ElementFilterRuleGreater()
    : base("Greater Rule", "Greater", "Filter used to match elements if value of a parameter greater than Value", "Revit", "Filter")
    { }
  }

  public class ElementFilterRuleGreaterOrEqual : ElementFilterRule
  {
    public override Guid ComponentGuid => new Guid("05BBAEDD-027B-40DA-8390-F826B63FD100");
    protected override string IconTag => "≥";
    protected override ConditionType Condition => ConditionType.GreaterOrEqual;

    public ElementFilterRuleGreaterOrEqual()
    : base("Greater Or Equal Rule", "GrtOrEqu", "Filter used to match elements if value of a parameter greater or equal than Value", "Revit", "Filter")
    { }
  }

  public class ElementFilterRuleLess : ElementFilterRule
  {
    public override Guid ComponentGuid => new Guid("BE2C5AFE-7D56-4F63-9A23-20560E3675B9");
    protected override string IconTag => "<";
    protected override ConditionType Condition => ConditionType.Less;

    public ElementFilterRuleLess()
    : base("Less Rule", "Less", "Filter used to match elements if value of a parameter less than Value", "Revit", "Filter")
    { }
  }

  public class ElementFilterRuleLessOrEqual : ElementFilterRule
  {
    public override Guid ComponentGuid => new Guid("BB69852F-6A39-4ADC-B9B8-D16A8862B4C7");
    protected override string IconTag => "≤";
    protected override ConditionType Condition => ConditionType.LessOrEqual;

    public ElementFilterRuleLessOrEqual()
    : base("Less Or Equal Rule", "LessOrEqu", "Filter used to match elements if value of a parameter less or equal than Value", "Revit", "Filter")
    { }
  }

  public abstract class ElementFilterStringRule : Component
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary;
    public override bool IsPreviewCapable => false;

    protected enum ConditionType
    {
      Contains,
      BeginsWith,
      EndsWith,
    }

    protected abstract ConditionType Condition { get; }

    protected ElementFilterStringRule(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ParameterKey(), "ParameterKey", "K", "Parameter to check", GH_ParamAccess.item);
      manager.AddTextParameter("Value", "V", "Value to check with", GH_ParamAccess.item);
      manager.AddBooleanParameter("Inverted", "I", "True if the results of the rule should be inverted", GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.FilterRule(), "Rule", "R", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var parameterKey = default(Types.ParameterKey);
      if (!DA.GetData("ParameterKey", ref parameterKey))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      if (!ElementFilterRule.TryGetParameterDefinition(parameterKey.Document, parameterKey.Id, out var storageType, out var dataType))
      {
        if (parameterKey.Id.TryGetBuiltInParameter(out var builtInParameter))
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to found parameter '{DB.LabelUtils.GetLabelFor(builtInParameter)}' in Revit document.");
        else
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to found parameter '{parameterKey.Id.IntegerValue}' in Revit document.");

        return;
      }

      if (storageType != DB.StorageType.String)
      {
        if (parameterKey.Id.TryGetBuiltInParameter(out var builtInParameter))
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter '{DB.LabelUtils.GetLabelFor(builtInParameter)}' is not a text parameter.");
        else
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter '{parameterKey.Id.IntegerValue}' is not a text parameter.");

        return;
      }

      var provider = new DB.ParameterValueProvider(parameterKey.Id);

      DB.FilterRule rule = null;
      if (storageType == DB.StorageType.String)
      {
        DB.FilterStringRuleEvaluator ruleEvaluator = null;
        switch (Condition)
        {
          case ConditionType.Contains: ruleEvaluator = new DB.FilterStringContains(); break;
          case ConditionType.BeginsWith: ruleEvaluator = new DB.FilterStringBeginsWith(); break;
          case ConditionType.EndsWith: ruleEvaluator = new DB.FilterStringEndsWith(); break;
        }

        var goo = default(GH_String);
        if (DA.GetData("Value", ref goo))
          rule = new DB.FilterStringRule(provider, ruleEvaluator, goo.Value, true);
      }

      if (rule is object)
      {
        if (inverted)
          DA.SetData("Rule", new DB.FilterInverseRule(rule));
        else
          DA.SetData("Rule", rule);
      }
    }
  }

  public class ElementFilterRuleContains : ElementFilterStringRule
  {
    public override Guid ComponentGuid => new Guid("B1265CF6-3031-4E05-B958-38D00C5A41EF");
    protected override string IconTag => "?";
    protected override ConditionType Condition => ConditionType.Contains;

    public ElementFilterRuleContains()
    : base("String Contains", "Contains", "Filter used to match elements if value of a parameter contains a string", "Revit", "Filter")
    { }
  }

  public class ElementFilterRuleBeginsWith : ElementFilterStringRule
  {
    public override Guid ComponentGuid => new Guid("7FA73840-6511-49BD-A4C9-85F0DFD907E5");
    protected override string IconTag => "<";
    protected override ConditionType Condition => ConditionType.BeginsWith;

    public ElementFilterRuleBeginsWith()
    : base("String Begins", "Begins", "Filter used to match elements if value of a parameter begins with a string", "Revit", "Filter")
    { }
  }

  public class ElementFilterRuleEndsWith : ElementFilterStringRule
  {
    public override Guid ComponentGuid => new Guid("84F29564-1ACD-4148-B00F-EA3FCFB6DF13");
    protected override string IconTag => ">";
    protected override ConditionType Condition => ConditionType.EndsWith;

    public ElementFilterRuleEndsWith()
    : base("String Ends", "Ends", "Filter used to match elements if value of a parameter ends with a string", "Revit", "Filter")
    { }
  }

  public class CategoryFilterRule : Component
  {
    public override Guid ComponentGuid => new Guid("0CE4F51D-49D0-4B0C-82C8-84CCCF0968F6");

    public override GH_Exposure Exposure => GH_Exposure.quinary | GH_Exposure.obscure;
    public override bool IsPreviewCapable => false;

    public CategoryFilterRule()
    : base("Category Rule", "Category", "Filter used to match elements on a category", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "Categories", "C", "Categories to check", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.FilterRule(), "Rule", "R", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var categories = new List<Types.Category>();
      if (!DA.GetDataList("Categories", categories) || categories.Count == 0)
        return;

      var categoryIds = categories.Select(x => x.Id).ToList();
      var rule = new DB.FilterCategoryRule(categoryIds);

      DA.SetData("Rule", rule);
    }
  }
  #endregion
}

namespace RhinoInside.Revit.GH.Components.Obsolete
{
  [Obsolete("Obsolete since 2020-10-22")]
  public class ElementLogicalAndFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("754C40D7-5AE8-4027-921C-0210BBDFAB37");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.hidden;
    protected override string IconTag => "∧";

    public ElementLogicalAndFilter()
    : base("Logical And Filter", "AndFltr", "Filter used to combine a set of filters that pass when any pass", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementFilter(), "Filters", "F", "Filters to combine", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var filters = new List<DB.ElementFilter>();
      if (!DA.GetDataList("Filters", filters))
        return;

      DA.SetData("Filter", new DB.LogicalAndFilter(filters));
    }
  }

  [Obsolete("Obsolete since 2020-10-22")]
  public class ElementLogicalOrFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("61F75DE1-EE65-4AA8-B9F8-40516BE46C8D");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.hidden;
    protected override string IconTag => "∨";

    public ElementLogicalOrFilter()
    : base("Logical Or Filter", "OrFltr", "Filter used to combine a set of filters that pass when any pass", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementFilter(), "Filters", "F", "Filters to combine", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var filters = new List<DB.ElementFilter>();
      if (!DA.GetDataList("Filters", filters))
        return;

      DA.SetData("Filter", new DB.LogicalOrFilter(filters));
    }
  }

  [Obsolete("Obsolete since 2020-10-15")]
  public class ElementBoundingBoxFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("F5A32842-B18E-470F-8BD3-BAE1373AD982");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.hidden;
    protected override string IconTag => "B";

    public ElementBoundingBoxFilter()
    : base("BoundingBox Filter", "BBoxFltr", "Filter used to match elements by their BoundingBox", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddPointParameter("Points", "C", "Points to query", GH_ParamAccess.list);
      manager.AddNumberParameter("Tolerance", "T", "Tolerance used to query", GH_ParamAccess.item, 0.0);
      manager.AddBooleanParameter("BoundingBox", "B", "Query as a BoundingBox", GH_ParamAccess.item, true);
      manager.AddBooleanParameter("Strict", "S", "True means element should be strictly contained", GH_ParamAccess.item, false);
      base.RegisterInputParams(manager);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      base.RegisterOutputParams(manager);
      manager.AddBoxParameter("Target", "T", string.Empty, GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var points = new List<Rhino.Geometry.Point3d>();
      if (!DA.GetDataList("Points", points))
        return;

      var tolerance = 0.0;
      if (!DA.GetData("Tolerance", ref tolerance))
        return;

      var boundingBox = true;
      if (!DA.GetData("BoundingBox", ref boundingBox))
        return;

      var strict = true;
      if (!DA.GetData("Strict", ref strict))
        return;

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      var targets = new List<Rhino.Geometry.Box>();
      DB.ElementFilter filter = null;

      if (boundingBox)
      {
        var pointsBBox = new Rhino.Geometry.BoundingBox(points);
        {
          var box = new Rhino.Geometry.Box(pointsBBox);
          box.Inflate(tolerance);
          targets.Add(box);
        }

        if (strict)
          filter = new DB.BoundingBoxIsInsideFilter(pointsBBox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
        else
          filter = new DB.BoundingBoxIntersectsFilter(pointsBBox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
      }
      else
      {
        var filters = points.Select<Rhino.Geometry.Point3d, DB.ElementFilter>
        (
          x =>
          {
            var pointsBBox = new Rhino.Geometry.BoundingBox(x, x);
            {
              var box = new Rhino.Geometry.Box(pointsBBox);
              box.Inflate(tolerance);
              targets.Add(box);
            }

            if (strict)
            {
              return new DB.BoundingBoxIsInsideFilter(pointsBBox.ToOutline(), tolerance / Revit.ModelUnits, inverted);
            }
            else
            {
              return new DB.BoundingBoxContainsPointFilter(x.ToXYZ(), tolerance / Revit.ModelUnits, inverted);
            }
          }
        );

        var filterList = filters.ToArray();
        filter = filterList.Length == 1 ?
                 filterList[0] :
                 new DB.LogicalOrFilter(filterList);
      }

      DA.SetData("Filter", filter);
      DA.SetDataList("Target", targets);
    }
  }
}
