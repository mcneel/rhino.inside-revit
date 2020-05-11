using System;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentBasicWallTypes : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("E37BB2DB-E096-45A1-9771-94CE7DBCCDB8");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "BW";

    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.WallType));

    public DocumentBasicWallTypes() : base
    (
      name: "Basic Wall Types",
      nickname: "Basic WallTypes",
      description: "Get document basic wall types list",
      category: "Revit",
      subCategory: "Query"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      base.RegisterInputParams(manager);

      manager[manager.AddTextParameter("Name", "N", "Wall Type name", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddParameter(new Parameters.Param_Enum<Types.WallFunction>(), "Function", "F", string.Empty, GH_ParamAccess.item)].Optional = true;
      manager[manager.AddIntervalParameter("Width", "W", string.Empty, GH_ParamAccess.item)].Optional = true;
      manager[manager.AddParameter(new Parameters.ElementFilter(), "Filter", "F", "Filter", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "Types", "W", "Basic wall Types list", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      string name = null;
      DA.GetData("Name", ref name);

      var wallFunction = DB.WallFunction.Interior;
      bool filterFunction = DA.GetData("Function", ref wallFunction);

      var width = Rhino.Geometry.Interval.Unset;
      DA.GetData("Width", ref width);

      var filter = default(DB.ElementFilter);
      DA.GetData("Filter", ref filter);

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var elementCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          elementCollector = elementCollector.WherePasses(filter);

        if (TryGetFilterStringParam(DB.BuiltInParameter.SYMBOL_NAME_PARAM, ref name, out var nameFilter))
          elementCollector = elementCollector.WherePasses(nameFilter);

        if (filterFunction)
        {
          if (TryGetFilterIntegerParam(DB.BuiltInParameter.FUNCTION_PARAM, (int) wallFunction, out var functionFilter))
            elementCollector = elementCollector.WherePasses(functionFilter);
        }

        if (width.IsValid && TryGetFilterDoubleParam(DB.BuiltInParameter.WALL_ATTR_WIDTH_PARAM, width.Mid / Revit.ModelUnits, Revit.VertexTolerance + (width.Length * 0.5 / Revit.ModelUnits), out var widthFilter))
          elementCollector = elementCollector.WherePasses(widthFilter);

        var elements = collector.Cast<DB.WallType>();

        elements = elements.Where(x => x.Kind == DB.WallKind.Basic);

        if (!string.IsNullOrEmpty(name))
          elements = elements.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList("Types", elements);
      }
    }
  }

  public class DocumentCurtainWallTypes : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("E49700B5-D16A-4D18-9EA7-C03AD64CF03D");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "CW";

    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.WallType));

    public DocumentCurtainWallTypes() : base
    (
      name: "Curtain Wall Types",
      nickname: "CurtainWallTypes",
      description: "Get document curatin wall types list",
      category: "Revit",
      subCategory: "Query"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      base.RegisterInputParams(manager);

      manager[manager.AddTextParameter("Name", "N", "Wall Type name", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddParameter(new Parameters.Param_Enum<Types.WallFunction>(), "Function", "F", string.Empty, GH_ParamAccess.item)].Optional = true;
      manager[manager.AddIntervalParameter("Width", "W", string.Empty, GH_ParamAccess.item)].Optional = true;
      manager[manager.AddParameter(new Parameters.ElementFilter(), "Filter", "F", "Filter", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "Types", "W", "Curtain wall Types list", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      string name = null;
      DA.GetData("Name", ref name);

      var wallFunction = DB.WallFunction.Interior;
      bool filterFunction = DA.GetData("Function", ref wallFunction);

      var width = Rhino.Geometry.Interval.Unset;
      DA.GetData("Width", ref width);

      var filter = default(DB.ElementFilter);
      DA.GetData("Filter", ref filter);

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var elementCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          elementCollector = elementCollector.WherePasses(filter);

        if (TryGetFilterStringParam(DB.BuiltInParameter.SYMBOL_NAME_PARAM, ref name, out var nameFilter))
          elementCollector = elementCollector.WherePasses(nameFilter);

        if (filterFunction)
        {
          if (TryGetFilterIntegerParam(DB.BuiltInParameter.FUNCTION_PARAM, (int) wallFunction, out var functionFilter))
            elementCollector = elementCollector.WherePasses(functionFilter);
        }

        var elements = collector.Cast<DB.WallType>();
        elements = elements.Where(x => x.Kind == DB.WallKind.Curtain);

        if (!string.IsNullOrEmpty(name))
          elements = elements.Where(x => x.Name.IsSymbolNameLike(name));

        elements = elements.Where(x => width.IncludesParameter(x.Width * Revit.ModelUnits));

        DA.SetDataList("Types", elements);
      }
    }
  }

  public class DocumentStackedWallTypes : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("82C96794-CD04-444E-A70B-50D2E3F2725D");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "SW";

    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.WallType));

    public DocumentStackedWallTypes() : base
    (
      name: "Stacked Wall Types",
      nickname: "StackedWallTypes",
      description: "Get document curatin stacked types list",
      category: "Revit",
      subCategory: "Query"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      base.RegisterInputParams(manager);

      manager[manager.AddTextParameter("Name", "N", "Wall Type name", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddParameter(new Parameters.Param_Enum<Types.WallFunction>(), "Function", "F", string.Empty, GH_ParamAccess.item)].Optional = true;
      manager[manager.AddIntervalParameter("Width", "W", string.Empty, GH_ParamAccess.item)].Optional = true;
      manager[manager.AddParameter(new Parameters.ElementFilter(), "Filter", "F", "Filter", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "Types", "W", "Curtain wall Types list", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      string name = null;
      DA.GetData("Name", ref name);

      var wallFunction = DB.WallFunction.Interior;
      bool filterFunction = DA.GetData("Function", ref wallFunction);

      var width = Rhino.Geometry.Interval.Unset;
      DA.GetData("Width", ref width);

      var filter = default(DB.ElementFilter);
      DA.GetData("Filter", ref filter);

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var elementCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          elementCollector = elementCollector.WherePasses(filter);

        if (TryGetFilterStringParam(DB.BuiltInParameter.SYMBOL_NAME_PARAM, ref name, out var nameFilter))
          elementCollector = elementCollector.WherePasses(nameFilter);

        if (filterFunction)
        {
          if (TryGetFilterIntegerParam(DB.BuiltInParameter.FUNCTION_PARAM, (int) wallFunction, out var functionFilter))
            elementCollector = elementCollector.WherePasses(functionFilter);
        }

        var elements = collector.Cast<DB.WallType>();
        elements = elements.Where(x => x.Kind == DB.WallKind.Stacked);

        if (!string.IsNullOrEmpty(name))
          elements = elements.Where(x => x.Name.IsSymbolNameLike(name));

        elements = elements.Where(x => width.IncludesParameter(x.Width * Revit.ModelUnits));

        DA.SetDataList("Types", elements);
      }
    }
  }
}
