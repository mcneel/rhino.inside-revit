using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
  [Obsolete("Obsolete since 2020-06-01")]
  public class QueryWallTypes : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("E37BB2DB-E096-45A1-9771-94CE7DBCCDB8");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override string IconTag => "W";

    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementClassFilter(typeof(ARDB.WallType));

    public QueryWallTypes() : base
    (
      name: "Query Wall Types",
      nickname: "WallTypes",
      description: "Get document wall types list",
      category: "Revit",
      subCategory: "Wall"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Name", "N", "Wall Type name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.Param_Enum<Types.WallFunction>>("Function", "F", string.Empty, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_Interval>("Width", "W", string.Empty, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ElementType>("Types", "T", "Wall Types list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      var wallKind = ARDB.WallKind.Unknown;
      DA.GetData("Family", ref wallKind);

      string name = null;
      DA.GetData("Name", ref name);

      var wallFunction = ARDB.WallFunction.Interior;
      bool filterFunction = DA.GetData("Function", ref wallFunction);

      var width = Rhino.Geometry.Interval.Unset;
      DA.GetData("Width", ref width);

      var filter = default(ARDB.ElementFilter);
      DA.GetData("Filter", ref filter);

      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var elementCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          elementCollector = elementCollector.WherePasses(filter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.ALL_MODEL_TYPE_NAME, ref name, out var nameFilter))
          elementCollector = elementCollector.WherePasses(nameFilter);

        if (filterFunction)
        {
          if (TryGetFilterIntegerParam(ARDB.BuiltInParameter.FUNCTION_PARAM, (int) wallFunction, out var functionFilter))
            elementCollector = elementCollector.WherePasses(functionFilter);
        }

        // DB.BuiltInParameter.WALL_ATTR_WIDTH_PARAM only works with Basic wall types
        if (width.IsValid && wallKind == ARDB.WallKind.Basic && TryGetFilterDoubleParam(ARDB.BuiltInParameter.WALL_ATTR_WIDTH_PARAM, width.Mid / Revit.ModelUnits, Revit.VertexTolerance + (width.Length * 0.5 / Revit.ModelUnits), out var widthFilter))
          elementCollector = elementCollector.WherePasses(widthFilter);

        var elements = collector.Cast<ARDB.WallType>();

        if (wallKind != ARDB.WallKind.Unknown)
          elements = elements.Where(x => x.Kind == wallKind);

        if (!string.IsNullOrEmpty(name))
          elements = elements.Where(x => x.Name.IsSymbolNameLike(name));

        if (width.IsValid && wallKind != ARDB.WallKind.Basic)
          elements = elements.Where(x => width.IncludesParameter(x.Width * Revit.ModelUnits));

        DA.SetDataList
        (
          "Types",
          elements.
          Select(Types.ElementType.FromElement).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
