using System;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentWallTypes : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("E37BB2DB-E096-45A1-9771-94CE7DBCCDB8");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "W";

    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.WallType));

    public DocumentWallTypes() : base
    (
      name: "Document WallTypes",
      nickname: "WallTypes",
      description: "Get document wall types list",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      base.RegisterInputParams(manager);

      manager[manager.AddParameter(new Parameters.Param_Enum<Types.WallSystemFamily>(), "Family", "F", string.Empty, GH_ParamAccess.item)].Optional = true;
      manager[manager.AddTextParameter("Name", "N", "Wall Type name", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddParameter(new Parameters.Param_Enum<Types.WallFunction>(), "Function", "F", string.Empty, GH_ParamAccess.item)].Optional = true;
      manager[manager.AddIntervalParameter("Width", "W", string.Empty, GH_ParamAccess.item)].Optional = true;
      manager[manager.AddParameter(new Parameters.ElementFilter(), "Filter", "F", "Filter", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ElementType(), "WallTypes", "W", "Wall Types list", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      var wallKind = DB.WallKind.Unknown;
      DA.GetData("Family", ref wallKind);

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

        // DB.BuiltInParameter.WALL_ATTR_WIDTH_PARAM only works with Basic wall types
        if (width.IsValid && wallKind == DB.WallKind.Basic && TryGetFilterDoubleParam(DB.BuiltInParameter.WALL_ATTR_WIDTH_PARAM, width.Mid / Revit.ModelUnits, Revit.VertexTolerance + (width.Length * 0.5 / Revit.ModelUnits), out var widthFilter))
          elementCollector = elementCollector.WherePasses(widthFilter);

        var elements = collector.Cast<DB.WallType>();

        if(wallKind != DB.WallKind.Unknown)
          elements = elements.Where(x => x.Kind == wallKind);

        if (!string.IsNullOrEmpty(name))
          elements = elements.Where(x => x.Name.IsSymbolNameLike(name));

        if (width.IsValid && wallKind != DB.WallKind.Basic)
          elements = elements.Where(x => width.IncludesParameter(x.Width * Revit.ModelUnits));

        DA.SetDataList("WallTypes", elements);
      }
    }
  }
}
