using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentLevels : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("87715CAF-92A9-4B14-99E5-F8CCB2CC19BD");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.Level));

    public DocumentLevels() : base
    (
      name: "Query Levels",
      nickname: "Levels",
      description: "Get all document levels",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.FromParam(new Parameters.Document(), ParamVisibility.Voluntary),
      ParamDefinition.Create<Param_Interval>("Elevation", "E", "Level elevation interval", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_String>("Name", "N", "Level name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_Boolean>("Structural", "S", "Level is structural", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_Boolean>("Building Story", "BS", "Level is building story", defaultValue: true, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Level>("Levels", "L", "Levels list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      if (DA.TryGetData(Params.Input, "Elevation", out Interval? elevation) && !elevation.Value.IsValid)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Elevation value is not valid.");
        return;
      }
      DA.TryGetData(Params.Input, "Name", out string name);
      DA.TryGetData(Params.Input, "Structural", out bool? structural);
      DA.TryGetData(Params.Input, "Building Story", out bool? buildingStory);
      DA.TryGetData(Params.Input, "Filter", out DB.ElementFilter filter);

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var levelsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          levelsCollector = levelsCollector.WherePasses(filter);

        if (elevation.HasValue && TryGetFilterDoubleParam(DB.BuiltInParameter.LEVEL_ELEV, elevation.Value.Mid / Revit.ModelUnits, Revit.VertexTolerance + (elevation.Value.Length * 0.5 / Revit.ModelUnits), out var elevationFilter))
          levelsCollector = levelsCollector.WherePasses(elevationFilter);

        if (TryGetFilterStringParam(DB.BuiltInParameter.DATUM_TEXT, ref name, out var nameFilter))
          levelsCollector = levelsCollector.WherePasses(nameFilter);

        if (structural.HasValue && TryGetFilterIntegerParam(DB.BuiltInParameter.LEVEL_IS_STRUCTURAL, structural.Value ? 1 : 0, out var structuralFilter))
          levelsCollector = levelsCollector.WherePasses(structuralFilter);

        if (buildingStory.HasValue && TryGetFilterIntegerParam(DB.BuiltInParameter.LEVEL_IS_BUILDING_STORY, buildingStory.Value ? 1 : 0, out var buildingStoryilter))
          levelsCollector = levelsCollector.WherePasses(buildingStoryilter);

        var levels = levelsCollector.Cast<DB.Level>();

        if (!string.IsNullOrEmpty(name))
          levels = levels.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList("Levels", levels);
      }
    }
  }
}
