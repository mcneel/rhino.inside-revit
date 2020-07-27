using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

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
      var elements = new List<Types.Element>();
      if (!DA.GetDataList("Elements", elements))
        return;

      var filter = default(DB.ElementFilter);
      if (!DA.GetData("Filter", ref filter))
        return;

      var pass = new List<bool>(elements.Count);
      foreach (var element in elements)
        pass.Add(element.IsValid && filter.PassesFilter(element.Document, element.Id));

      DA.SetDataList("Pass", pass);
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
    : base("Exclusion Filter", "Exclusion", "Filter used to exclude a set of elements", "Revit", "Filter")
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
      else
        DA.DisableGapLogic();
    }
  }

  public class ElementLogicalAndFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("754C40D7-5AE8-4027-921C-0210BBDFAB37");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "∧";

    public ElementLogicalAndFilter()
    : base("Logical And Filter", "LogAnd", "Filter used to combine a set of filters that pass when any pass", "Revit", "Filter")
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

  public class ElementLogicalOrFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("61F75DE1-EE65-4AA8-B9F8-40516BE46C8D");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "∨";

    public ElementLogicalOrFilter()
    : base("Logical Or Filter", "LogOr", "Filter used to combine a set of filters that pass when any pass", "Revit", "Filter")
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
  #endregion

  #region Secondary
  public class ElementExcludeElementTypeFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("F69D485F-B262-4297-A496-93F5653F5D19");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "T";

    public ElementExcludeElementTypeFilter()
    : base("Exclude Types", "NotTypes", "Filter used to exclude element types", "Revit", "Filter")
    { }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      DA.SetData("Filter", new DB.ElementIsElementTypeFilter(!inverted));
    }
  }

  public class ElementClassFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("6BD34014-CD73-42D8-94DB-658BE8F42254");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    protected override string IconTag => "C";

    public ElementClassFilter()
    : base("Class Filter", "ByClass", "Filter used to match elements by their API class", "Revit", "Filter")
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
    : base("Category Filter", "ByCategory", "Filter used to match elements by their category", "Revit", "Filter")
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
    : base("Type Filter", "ByType", "Filter used to match elements by their type", "Revit", "Filter")
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
    : base("Parameter Filter", "ParaFilter", "Filter used to match elements by the value of a parameter", "Revit", "Filter")
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

      DA.SetData("Filter", new DB.ElementParameterFilter(rules, inverted));
    }
  }
  #endregion

  #region Tertiary
  public class ElementBoundingBoxFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("F5A32842-B18E-470F-8BD3-BAE1373AD982");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "B";

    public ElementBoundingBoxFilter()
    : base("BoundingBox Filter", "ByBBox", "Filter used to match elements by their BoundingBox", "Revit", "Filter")
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

  public class ElementIntersectsElementFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("D1E4C98D-E550-4F25-991A-5061EF845C37");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "I";

    public ElementIntersectsElementFilter()
    : base("Intersects Element Filter", "IsectsElement", "Filter used to match elements that intersect to the given element", "Revit", "Filter")
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
    : base("Intersects Brep Filter", "IsectsBrep", "Filter used to match elements that intersect to the given brep", "Revit", "Filter")
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

      DA.SetData("Filter", new DB.ElementIntersectsSolidFilter(brep.ToSolid(), inverted));
    }
  }

  public class ElementIntersectsMeshFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("09F9E451-F6C9-42FB-90E3-85E9923998A2");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "I";

    public ElementIntersectsMeshFilter()
    : base("Intersects Mesh Filter", "IsectsMesh", "Filter used to match elements that intersect to the given mesh", "Revit", "Filter")
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

      DA.SetData("Filter", new DB.ElementIntersectsSolidFilter(mesh.ToSolid(), inverted));
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
    : base("Level Filter", "ByLevel", "Filter used to match elements associated to the given level", "Revit", "Filter")
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
    : base("Design Option Filter", "ByDesignOption", "Filter used to match elements associated to the given Design Option", "Revit", "Filter")
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

  public class ElementOwnerViewFilter : ElementFilterComponent
  {
    public override Guid ComponentGuid => new Guid("CFB42D90-F9D4-4601-9EEF-C624E92A424D");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "V";

    public ElementOwnerViewFilter()
    : base("Owner View Filter", "ByOwnerView", "Filter used to match elements associated to the given View", "Revit", "Filter")
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
    : base("Selectable In View Filter", "SelecInView", "Filter used to match seletable elements into the given View", "Revit", "Filter")
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

    static readonly Dictionary<DB.BuiltInParameter, DB.ParameterType> BuiltInParametersTypes = new Dictionary<DB.BuiltInParameter, DB.ParameterType>();

    static bool TryGetParameterDefinition(DB.Document doc, DB.ElementId id, out DB.StorageType storageType, out DB.ParameterType parameterType)
    {
      if (id.TryGetBuiltInParameter(out var builtInParameter))
      {
        storageType = doc.get_TypeOfStorage(builtInParameter);

        if (storageType == DB.StorageType.ElementId)
        {
          if (builtInParameter == DB.BuiltInParameter.ELEM_TYPE_PARAM)
          {
            parameterType = DB.ParameterType.FamilyType;
            return true;
          }

          if (builtInParameter == DB.BuiltInParameter.ELEM_CATEGORY_PARAM || builtInParameter == DB.BuiltInParameter.ELEM_CATEGORY_PARAM_MT)
          {
            parameterType = (DB.ParameterType) int.MaxValue;
            return true;
          }
        }

        if (storageType == DB.StorageType.Double)
        {
          if (BuiltInParametersTypes.TryGetValue(builtInParameter, out parameterType))
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

                parameterType = parameter.Definition.ParameterType;
                BuiltInParametersTypes.Add(builtInParameter, parameterType);
                return true;
              }
            }
          }

          parameterType = DB.ParameterType.Invalid;
          return false;
        }

        parameterType = DB.ParameterType.Invalid;
        return true;
      }
      else
      {
        try
        {
          if (doc.GetElement(id) is DB.ParameterElement parameter)
          {
            storageType = parameter.GetDefinition().ParameterType.ToStorageType();
            parameterType = parameter.GetDefinition().ParameterType;
            return true;
          }
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }
      }

      storageType = DB.StorageType.None;
      parameterType = DB.ParameterType.Invalid;
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

      DA.DisableGapLogic();

      if (!TryGetParameterDefinition(parameterKey.Document, parameterKey.Id, out var storageType, out var parameterType))
      {
        if (parameterKey.Id.TryGetBuiltInParameter(out var builtInParameter))
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to found parameter '{DB.LabelUtils.GetLabelFor(builtInParameter)}' in Revit document.");
        else
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to found parameter '{parameterKey.Id.IntegerValue}' in Revit document.");

        return;
      }

      var provider = new DB.ParameterValueProvider(parameterKey);

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
                if (Condition == ConditionType.Equals || Condition == ConditionType.NotEquals)
                {
                  if (parameterType == DB.ParameterType.Length || parameterType == DB.ParameterType.Area || parameterType == DB.ParameterType.Volume)
                    rule = new DB.FilterDoubleRule(provider, ruleEvaluator, UnitConverter.InHostUnits(goo.Value, parameterType), UnitConverter.InHostUnits(Revit.VertexTolerance, parameterType));
                  else
                    rule = new DB.FilterDoubleRule(provider, ruleEvaluator, UnitConverter.InHostUnits(goo.Value, parameterType), 1e-6);
                }
                else
                  rule = new DB.FilterDoubleRule(provider, ruleEvaluator, UnitConverter.InHostUnits(goo.Value, parameterType), 0.0);
              }
            }
            break;
          case DB.StorageType.ElementId:
            {
              switch(parameterType)
              {
                case (DB.ParameterType) int.MaxValue: // Category
                  {
                    var value = default(Types.Category);
                    if (DA.GetData("Value", ref value))
                      rule = new DB.FilterElementIdRule(provider, ruleEvaluator, value);
                  }
                  break;
                case DB.ParameterType.Material:
                  {
                    var value = default(Types.Material);
                    if (DA.GetData("Value", ref value))
                      rule = new DB.FilterElementIdRule(provider, ruleEvaluator, value);
                  }
                  break;
                case DB.ParameterType.FamilyType:
                  {
                    var value = default(Types.ElementType);
                    if (DA.GetData("Value", ref value))
                      rule = new DB.FilterElementIdRule(provider, ruleEvaluator, value);
                  }
                  break;
                default:
                  {
                    var value = default(Types.Element);
                    if (DA.GetData("Value", ref value))
                      rule = new DB.FilterElementIdRule(provider, ruleEvaluator, value);
                  }
                  break;
              }
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
  #endregion
}
