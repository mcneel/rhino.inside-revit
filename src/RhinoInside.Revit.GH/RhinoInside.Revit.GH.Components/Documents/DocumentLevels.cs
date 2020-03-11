using System;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Documents
{
  public class DocumentLevels : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("87715CAF-92A9-4B14-99E5-F8CCB2CC19BD");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.Level));

    public DocumentLevels() : base
    (
      "Document.Levels", "Levels",
      "Get active document levels list",
      "Revit", "Document"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      base.RegisterInputParams(manager);

      manager[manager.AddIntervalParameter("Elevation", "E", "Level elevation interval", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddTextParameter("Name", "N", "Level name", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddParameter(new Parameters.Documents.Filters.ElementFilter(), "Filter", "F", "Filter", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Elements.Level.Level(), "Levels", "Levels", "Levels list", GH_ParamAccess.list);
    }

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

        var levels = levelsCollector.Cast<DB.Level>();

        if (elevation.IsValid)
          levels = levels.Where(x => elevation.IncludesParameter(x.Elevation));

        if (!string.IsNullOrEmpty(name))
          levels = levels.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList("Levels", levels);
      }
    }
  }
}
