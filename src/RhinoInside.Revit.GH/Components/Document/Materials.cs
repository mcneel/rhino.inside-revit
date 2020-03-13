using System;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentMaterials : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("94AF13C1-CE70-46B5-9103-24B46E2F7375");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "M";

    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.Material));

    public DocumentMaterials() : base
    (
      "Document Materials", "Materials",
      "Get document materials list",
      "Revit", "Document"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      base.RegisterInputParams(manager);

      manager[manager.AddTextParameter("Class", "C", "Material class", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddTextParameter("Name", "N", "Material name", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddParameter(new Parameters.ElementFilter(), "Filter", "F", "Filter", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Material(), "Materials", "Materials", "Materials list", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      string @class = null;
      DA.GetData("Class", ref @class);

      string name = null;
      DA.GetData("Name", ref name);

      DB.ElementFilter filter = null;
      DA.GetData("Filter", ref filter);

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var viewsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          viewsCollector = viewsCollector.WherePasses(filter);

        var materials = collector.Cast<DB.Material>();

        if (!string.IsNullOrEmpty(@class))
          materials = materials.Where(x => x.MaterialClass.IsSymbolNameLike(@class));

        if (!string.IsNullOrEmpty(name))
          materials = materials.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList("Materials", materials);
      }
    }
  }
}
