using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;
using DBX = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentFloorTypes : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("E3173EB7-2D53-4F81-BEB1-90D0F47343D4");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "F";

    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.FloorType));

    public DocumentFloorTypes() : base
    (
      name: "Floor Types",
      nickname: "FloorTypes",
      description: "Get document floor types list",
      category: "Revit",
      subCategory: "Query"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.FromParam(DocumentComponent.CreateDocumentParam(), ParamVisibility.Voluntary),
      //ParamDefinition.Create<Parameters.Category>("Category", "C",string .Empty, GH_ParamAccess.item, optional: true),
      //ParamDefinition.Create<Param_String>("Family Name", "FN", string.Empty, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_String>("Name", "N", string.Empty, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.Param_Enum<Types.FloorFunction>>("Function", "F", string.Empty, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_Interval>("Default Thickness", "T", string.Empty, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter","F", "Filter",GH_ParamAccess.item ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ElementType>("Types", "T", "Types list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      string name = null;
      DA.GetData("Name", ref name);

      var floorFunction = default(DBX.FloorFunction);
      bool filterFunction = DA.GetData("Function", ref floorFunction);

      var defaultThickness = Rhino.Geometry.Interval.Unset;
      DA.GetData("Default Thickness", ref defaultThickness);

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
          if (TryGetFilterIntegerParam(DB.BuiltInParameter.FUNCTION_PARAM, (int) floorFunction, out var functionFilter))
            elementCollector = elementCollector.WherePasses(functionFilter);
        }

        if (defaultThickness.IsValid && TryGetFilterDoubleParam(DB.BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM, defaultThickness.Mid / Revit.ModelUnits, Revit.VertexTolerance + (defaultThickness.Length * 0.5 / Revit.ModelUnits), out var widthFilter))
          elementCollector = elementCollector.WherePasses(widthFilter);

        var elements = collector.Cast<DB.FloorType>();

        elements = elements.Where(x => x.IsFoundationSlab == false);

        if (!string.IsNullOrEmpty(name))
          elements = elements.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList("Types", elements);
      }
    }
  }

  public class DocumentFoundationSlabTypes : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("8472D50D-7F43-4A2C-828B-8E1C35313EFF");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "FS";

    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.FloorType));

    public DocumentFoundationSlabTypes() : base
    (
      name: "Foundation Slab Types",
      nickname: "FoundationSlabTypes",
      description: "Get document foundation slab types list",
      category: "Revit",
      subCategory: "Query"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.FromParam(DocumentComponent.CreateDocumentParam(), ParamVisibility.Voluntary),
      //ParamDefinition.Create<Parameters.Category>("Category", "C",string .Empty, GH_ParamAccess.item, optional: true),
      //ParamDefinition.Create<Param_String>("Family Name", "FN", string.Empty, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_String>("Name", "N", string.Empty, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_Interval>("Default Thickness", "T", string.Empty, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter","F", "Filter",GH_ParamAccess.item ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ElementType>("Types", "T", "Foundation Slab Types list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      string name = null;
      DA.GetData("Name", ref name);

      var defaultThickness = Rhino.Geometry.Interval.Unset;
      DA.GetData("Default Thickness", ref defaultThickness);

      var filter = default(DB.ElementFilter);
      DA.GetData("Filter", ref filter);

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var elementCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          elementCollector = elementCollector.WherePasses(filter);

        if (TryGetFilterStringParam(DB.BuiltInParameter.SYMBOL_NAME_PARAM, ref name, out var nameFilter))
          elementCollector = elementCollector.WherePasses(nameFilter);

        if (defaultThickness.IsValid && TryGetFilterDoubleParam(DB.BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM, defaultThickness.Mid / Revit.ModelUnits, Revit.VertexTolerance + (defaultThickness.Length * 0.5 / Revit.ModelUnits), out var widthFilter))
          elementCollector = elementCollector.WherePasses(widthFilter);

        var elements = collector.Cast<DB.FloorType>();

        elements = elements.Where(x => x.IsFoundationSlab == true);

        if (!string.IsNullOrEmpty(name))
          elements = elements.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList("Types", elements);
      }
    }
  }
}
