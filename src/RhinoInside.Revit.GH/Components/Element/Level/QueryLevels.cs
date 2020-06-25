using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
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
      ParamDefinition.FromParam(DocumentComponent.CreateDocumentParam(), ParamVisibility.Voluntary),
      ParamDefinition.Create<Param_Interval>("Elevation", "E", "Level elevation interval", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_String>("Name", "N", "Level name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Level>("Levels", "L", "Levels list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      var elevation = Rhino.Geometry.Interval.Unset;
      DA.GetData("Elevation", ref elevation);

      string name = null;
      DA.GetData("Name", ref name);

      DB.ElementFilter filter = null;
      DA.GetData("Filter", ref filter);

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var levelsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          levelsCollector = levelsCollector.WherePasses(filter);

        if (TryGetFilterStringParam(DB.BuiltInParameter.DATUM_TEXT, ref name, out var nameFilter))
          levelsCollector = levelsCollector.WherePasses(nameFilter);

        if (elevation.IsValid && TryGetFilterDoubleParam(DB.BuiltInParameter.LEVEL_ELEV, elevation.Mid / Revit.ModelUnits, Revit.VertexTolerance + (elevation.Length * 0.5 / Revit.ModelUnits), out var elevationFilter))
          levelsCollector = levelsCollector.WherePasses(elevationFilter);

        var levels = levelsCollector.Cast<DB.Level>();

        if (!string.IsNullOrEmpty(name))
          levels = levels.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList("Levels", levels);
      }
    }
  }
}
